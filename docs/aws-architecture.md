# AWS Architecture Overview

This document describes the AWS services used in the **SkiResortConditionsDashboard** project, how they are connected, and what role each plays.

## Architecture Diagram

```
Browser / Angular SPA
        |
        | HTTPS
        v
 API Gateway HTTP API          (ski-resort-api)
   ANY /api/{proxy+}
   GET /
        |
        | HTTP → ALB DNS
        v
 Application Load Balancer     (ski-resort-alb)
   Listener: HTTP :80
   Target group: ski-resort-api-tg (IP, port 8080)
        |
        | port 8080
        v
 ECS Fargate Service           (ski-resort-cluster / ski-resort-api-service)
 ┌─────────────────────────────────────────────────────┐
 │  Task: ski-resort-api (Fargate, awsvpc, Linux/x64)  │
 │                                                     │
 │  Container 1: ski-resort-api          (port 8080)   │
 │    .NET 8 ASP.NET Core API                          │
 │    – REST endpoints                                 │
 │    – SignalR hub                                    │
 │    – SQS ingestion worker                           │
 │    – Emits OTLP → 127.0.0.1:4317                   │
 │                                                     │
 │  Container 2: adot-collector          (port 4317)   │
 │    AWS Distro for OpenTelemetry                     │
 │    – Receives OTLP from API container               │
 │    – Forwards traces/metrics → CloudWatch           │
 └─────────────────────────────────────────────────────┘
        |              |              |
        | port 5432    | AWS SDK      | AWS SDK
        v              v              v
   RDS PostgreSQL   DynamoDB       SQS Queue
   ski-resort-db    SkiResort      ski-resort-
                    UserFavorites  weather-ingestion
```

---

## Services and Their Roles

### 1. API Gateway HTTP API
- **Name:** `ski-resort-api`
- **Purpose:** Public HTTPS entry point that routes browser and client requests to the backend.
- **Routes:**
  - `ANY /api/{proxy+}` → forwards all API traffic to the ALB
  - `GET /` → health/root check
- **Why:** Provides a managed HTTPS endpoint with a stable invoke URL, without needing to manage SSL termination on the ALB directly for development.

---

### 2. Application Load Balancer (ALB)
- **Name:** `ski-resort-alb`
- **Scheme:** Internet-facing
- **Listener:** HTTP :80 → forwards to target group `ski-resort-api-tg`
- **Target group:** IP type, port 8080, health check `GET /`
- **Purpose:** Distributes incoming HTTP requests across healthy ECS Fargate tasks. Registers task private IPs automatically when the ECS service scales.
- **Security group (`ski-alb-sg`):** inbound HTTP 80 and HTTPS 443 from internet; outbound to ECS tasks on 8080.

---

### 3. ECS Fargate
- **Cluster:** `ski-resort-cluster`
- **Service:** `ski-resort-api-service` (Replica, desired tasks: 1)
- **Task definition:** `ski-resort-api` (family), `awsvpc` network mode
- **Purpose:** Runs the containerized .NET API without managing EC2 instances. ECS handles task placement, health monitoring, restarts, and deployment.
- **Security group (`ski-ecs-tasks-sg`):** inbound port 8080 from `ski-alb-sg` only; outbound HTTPS 443 (for Secrets Manager, ECR, CloudWatch) and PostgreSQL 5432 to RDS.

#### Container 1 — `ski-resort-api`
- Image from ECR (`ski-resort-api:latest`)
- Exposes port **8080**
- Runs the ASP.NET Core API with:
  - REST controllers (resorts, conditions, favorites, reports, settings)
  - SignalR hub (`/hubs/resort-conditions`) for real-time updates
  - SQS background worker polling `ski-resort-weather-ingestion`
- Key env vars: `ConnectionStrings__Postgres`, `SqsIngestion__QueueUrl`, `AWS__Region`, `ASPNETCORE_ENVIRONMENT=Production`
- Logs to CloudWatch log group `/ecs/ski-resort-api`

#### Container 2 — `adot-collector`
- Image: `public.ecr.aws/aws-observability/aws-otel-collector:latest`
- Exposes port **4317** (OTLP gRPC) — only reachable within the task via `127.0.0.1:4317`
- Receives OpenTelemetry traces and metrics from the API container and forwards them to CloudWatch
- Not essential — if it crashes, the API container keeps running
- Logs to CloudWatch log group `/ecs/ski-resort-api-adot`

---

### 4. Amazon ECR
- **Repository:** `ski-resort-api`
- **Purpose:** Stores the Docker image for the .NET API. ECS pulls the image from ECR when starting tasks.
- The `ecsTaskExecutionRole` grants ECS permission to pull from ECR.

---

### 5. RDS PostgreSQL
- **Identifier:** `ski-resort-db`
- **Engine:** PostgreSQL 17
- **Instance:** `db.t4g.micro` (burstable, 1 GiB RAM)
- **Storage:** 20 GiB gp2
- **Purpose:** Primary relational store for resort data.
  - Tables: `Resorts`, `SnowConditions`, `LiftStatuses`, `RunStatuses`
  - Accessed via **Entity Framework Core** (primary ORM) and **Dapper** (bulk/comparison reports)
- **Network:** Private — no public access. Only reachable from within the VPC.
- **Security group (`ski-rds-sg`):** inbound PostgreSQL 5432 from `ski-ecs-tasks-sg` only.
- **Credentials:** stored in **AWS Secrets Manager** (`ski-resort/rds-password`) and injected into the ECS task at startup.

---

### 6. DynamoDB
- **Table:** `SkiResortUserFavorites`
- **Keys:** Partition key `UserId` (String), Sort key `ResortId` (String)
- **Capacity:** On-demand
- **PITR:** Enabled (35-day recovery window)
- **Purpose:** Stores per-user saved resort favorites. Chosen for its schemaless, low-latency key-value access pattern — no joins needed for favorites lookups.
- **Accessed by:** `UserFavoritesRepository` via the AWS SDK (`IAmazonDynamoDB`). Falls back to in-memory store if AWS credentials are not present (local dev).

---

### 7. SQS
- **Queue:** `ski-resort-weather-ingestion`
- **Type:** Standard (at-least-once delivery)
- **Purpose:** Decouples weather/snow data ingestion from the API. External producers (weather data jobs) publish messages to the queue; the `SqsIngestionWorker` background service inside the API container polls and processes them, persisting new `SnowCondition` records to RDS and notifying connected clients via SignalR.
- **Worker config:** `SqsIngestion__QueueUrl` env var, long-polling (10s wait time), up to 10 messages per receive.

---

### 8. AWS Secrets Manager
- **Secret:** `ski-resort/rds-password`
- **Purpose:** Stores the RDS master password (full connection string as a key/value pair). The ECS task definition references the secret ARN so the plain password is never stored in the task definition JSON or environment variables in plain text.
- **IAM:** `ecsTaskExecutionRole` has `secretsmanager:GetSecretValue` permission on this secret. The secret is fetched by the ECS agent **before** the container starts.
- **VPC Endpoint:** `ski-resort-endpoint` (Interface type, `com.amazonaws.us-east-1.secretsmanager`) ensures the secret fetch stays within the VPC without going over the public internet.

---

### 9. CloudWatch
- **Log groups:**
  - `/ecs/ski-resort-api` — application logs from the .NET API container
  - `/ecs/ski-resort-api-adot` — logs from the ADOT collector sidecar
- **Telemetry:** The API exports OpenTelemetry traces and metrics via OTLP to the ADOT sidecar on `127.0.0.1:4317`. The sidecar forwards them to CloudWatch using the default ECS config (`ecs-default-config.yaml`).
- **Instrumentation:** ASP.NET Core, HttpClient, Entity Framework Core, and runtime metrics are all instrumented via OpenTelemetry SDK packages.

---

### 10. IAM Roles

| Role | Used by | Key permissions |
|------|---------|-----------------|
| `ecsTaskExecutionRole` | ECS agent (pull image, start container) | ECR pull, CloudWatch Logs write, Secrets Manager read |
| `ski-resort-api-task-role` | Running container (AWS SDK calls) | SQS receive/delete on `ski-resort-weather-ingestion`, DynamoDB CRUD on `SkiResortUserFavorites` |

---

### 11. VPC and Networking

All resources share the same VPC (`vpc-0e75344a056ebc488`, `172.30.0.0/16`).

| Resource | Subnet | Public IP |
|----------|--------|-----------|
| ALB | Public subnets (1a, 1b) | Yes (internet-facing) |
| ECS tasks | Public subnets (1a, 1b) | Yes (outbound for ECR/Secrets) |
| RDS | Private (VPC only) | No |

**Security group chain:**

```
Internet → ski-alb-sg (80/443) → ski-ecs-tasks-sg (8080) → ski-rds-sg (5432)
```

**VPC Interface Endpoint** (`ski-resort-endpoint`) for Secrets Manager keeps secret fetches private within the VPC.

---

## Data Flow Summary

### Normal API request
```
Browser → API Gateway → ALB → ECS task (port 8080) → RDS PostgreSQL
```

### Real-time update
```
SQS message → SqsIngestionWorker → ECS task → RDS (persist) → SignalR hub → Browser
```

### Favorites
```
Browser → API → ECS task → DynamoDB (SkiResortUserFavorites)
```

### Observability
```
ECS task → OTLP (127.0.0.1:4317) → adot-collector → CloudWatch Logs/Metrics/Traces
```
