# AWS Architecture Overview

This document describes the AWS services used in the **SkiResortConditionsDashboard** project, how they are connected, and what role each plays.

## Architecture Diagram

```
Browser / Angular SPA
        |
        | HTTPS (page load)          HTTPS (API calls)
        v                                    |
 CloudFront CDN                              v
   dnjatcqx268s0.cloudfront.net    API Gateway HTTP API     (ski-resort-api)
   OAC → private S3 bucket           ANY /api/{proxy+}
   SPA fallback: 403/404→index.html  GET /
        |                                    |
        | S3 GetObject (OAC)                 | HTTP → ALB DNS
        v                                    | (overwrite:path=$request.path)
 S3 Bucket                                  v
   ski-resort-dashboard-frontend   Application Load Balancer (ski-resort-alb)
   index.html (no-cache)             Listener: HTTP :80
   *.js / *.css (1yr cache)          Target group: ski-resort-api-tg (IP, :8080)
                                             |
                                             | port 8080
                                             v
                              ECS Fargate Service  (ski-resort-cluster)
                              ┌──────────────────────────────────────────────┐
                              │  Task: ski-resort-api (Fargate, awsvpc)      │
                              │                                              │
                              │  Container 1: ski-resort-api   (port 8080)  │
                              │    .NET 8 ASP.NET Core API                  │
                              │    – REST endpoints                          │
                              │    – SignalR hub                             │
                              │    – SqsIngestionWorker (SQS → RDS)          │
                              │    – WeatherSyncWorker (Open-Meteo → RDS)    │
                              │    – Emits OTLP → 127.0.0.1:4317            │
                              │                                              │
                              │  Container 2: adot-collector   (port 4317)  │
                              │    AWS Distro for OpenTelemetry              │
                              │    – Receives OTLP from API container        │
                              │    – Forwards traces/metrics → CloudWatch    │
                              └──────────────────────────────────────────────┘
                                       |              |              |
                                       | port 5432    | AWS SDK      | AWS SDK
                                       v              v              v
                                  RDS PostgreSQL   DynamoDB       SQS Queue
                                  ski-resort-db    SkiResort      ski-resort-
                                                   UserFavorites  weather-ingestion
```

---

## Services and Their Roles

### 1. CloudFront CDN
- **Distribution:** `E7RT8Z8CIZJ5J`
- **Domain:** `https://dnjatcqx268s0.cloudfront.net`
- **Origin:** S3 bucket `ski-resort-dashboard-frontend` via Origin Access Control (OAC) — bucket is fully private
- **Purpose:** Serves the Angular SPA globally from edge locations. Handles HTTPS termination, HTTP/2, and compression. Acts as the public URL for the frontend.
- **SPA routing:** Custom error responses map both 403 and 404 → `/index.html` with HTTP 200, so Angular client-side routes (e.g. `/resorts`, `/favorites`) work on direct access or page refresh.
- **Cache strategy:**
  - Hashed JS/CSS chunks (`chunk-*.js`, `styles-*.css`) → `Cache-Control: public, max-age=31536000, immutable` (1 year)
  - `index.html` → `Cache-Control: no-cache, no-store, must-revalidate` (always fresh)
- **Price class:** `PriceClass_100` (US, EU, Asia) — free tier eligible

---

### 2. S3 Bucket
- **Bucket:** `ski-resort-dashboard-frontend`
- **Public access:** Fully blocked. Only CloudFront can read objects via OAC (`sigv4` signing).
- **Contents:** Output of `ng build --configuration production` (`dist/ski-resort-dashboard/browser/`)
- **Purpose:** Durable, low-cost static file store for the Angular build artifacts.
- **Redeployment:** `aws s3 sync` + `aws cloudfront create-invalidation --paths "/*"`

---

### 3. API Gateway HTTP API
- **Name:** `ski-resort-api`
- **Invoke URL:** `https://dl1iycu1qh.execute-api.us-east-1.amazonaws.com`
- **Purpose:** Public HTTPS entry point that routes browser API calls to the backend.
- **Routes:**
  - `ANY /api/{proxy+}` → forwards all API traffic to the ALB. Integration uses `overwrite:path=$request.path` to preserve the full original path.
  - `GET /` → health/root check
- **CORS:** Configured at the API Gateway level for browser `fetch`/XHR. **Re-check** `AllowOrigins` and `AllowCredentials` whenever you use session cookies or SignalR through the gateway; the ASP.NET app enables credentialed CORS for SignalR, so gateway and ALB integration must stay consistent with the SPA’s actual origin and negotiate URL.

---

### 4. Application Load Balancer (ALB)
- **Name:** `ski-resort-alb`
- **Scheme:** Internet-facing
- **Listener:** HTTP :80 → forwards to target group `ski-resort-api-tg`
- **Target group:** IP type, port 8080, health check `GET /`
- **Purpose:** Distributes incoming HTTP requests across healthy ECS Fargate tasks. Registers task private IPs automatically when the ECS service scales.
- **Security group (`ski-alb-sg`):** inbound HTTP 80 and HTTPS 443 from internet; outbound to ECS tasks on 8080.

---

### 5. ECS Fargate
- **Cluster:** `ski-resort-cluster`
- **Service:** `ski-resort-api-service-ipavsbd1` (Replica, desired tasks: 1)
- **Task definition:** `ski-resort-api` (family), `awsvpc` network mode
- **Purpose:** Runs the containerized .NET API without managing EC2 instances. ECS handles task placement, health monitoring, restarts, and deployment.
- **Security group (`ski-ecs-tasks-sg`):** inbound port 8080 from `ski-alb-sg` only; outbound HTTPS 443 (for Secrets Manager, ECR, CloudWatch) and PostgreSQL 5432 to RDS.

#### Container 1 — `ski-resort-api`
- Image from ECR (`ski-resort-api:latest`)
- Exposes port **8080**
- Runs the ASP.NET Core API with:
  - REST: `ResortsController` (list resorts, `GET .../conditions` with keyset pagination, `GET .../snow-history/stream`), `FavoritesController`, `ReportsController`, `SettingsController`
  - SignalR hub (`/hubs/resort-conditions`) for real-time updates
  - `SqsIngestionWorker` — polls `ski-resort-weather-ingestion`, persists `SnowCondition`, notifies via SignalR
  - `WeatherSyncWorker` — on a timer, calls **Open-Meteo** (`https://api.open-meteo.com`) for resorts with coordinates, persists enriched conditions, notifies via SignalR
- Key env vars: `ConnectionStrings__Postgres`, `SqsIngestion__QueueUrl`, `AWS__Region`, `ASPNETCORE_ENVIRONMENT=Production`, `Favorites__UseInMemoryRepository=false` (use DynamoDB in AWS), optional `Favorites__DynamoDbTableName`
- **SignalR scale-out:** optional `ConnectionStrings__Redis` or `SignalR__RedisConnectionString` (Stack Exchange Redis backplane) when ECS desired count > 1
- **CORS:** `app.UseCors()` uses `AllowCredentials()` for SignalR negotiate. Ensure **API Gateway** CORS (if used) matches how the SPA calls the API (origins, `AllowCredentials`, and WebSocket upgrade behavior for SignalR).
- Logs to CloudWatch log group `/ecs/ski-resort-api`

#### Container 2 — `adot-collector`
- Image: `public.ecr.aws/aws-observability/aws-otel-collector:latest`
- Exposes port **4317** (OTLP gRPC) — only reachable within the task via `127.0.0.1:4317`
- Receives OpenTelemetry traces and metrics from the API container and forwards them to CloudWatch
- Not essential — if it crashes, the API container keeps running
- Logs to CloudWatch log group `/ecs/ski-resort-api-adot`

---

### 6. Amazon ECR
- **Repository:** `ski-resort-api`
- **Purpose:** Stores the Docker image for the .NET API. ECS pulls the image from ECR when starting tasks.
- The `ecsTaskExecutionRole` grants ECS permission to pull from ECR.

---

### 7. RDS PostgreSQL
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

### 8. DynamoDB
- **Table:** `SkiResortUserFavorites`
- **Keys:** Partition key `UserId` (String), Sort key `ResortId` (String)
- **Capacity:** On-demand
- **PITR:** Enabled (35-day recovery window)
- **Purpose:** Stores per-user saved resort favorites. Chosen for its schemaless, low-latency key-value access pattern — no joins needed for favorites lookups.
- **Accessed by:** `DynamoDbUserFavoritesRepository` (`IAmazonDynamoDB`) when `Favorites:UseInMemoryRepository` is **false** (typical in Production on ECS). When **true** (default in local Development), the API uses `InMemoryUserFavoritesRepository` instead—no DynamoDB calls.

---

### 9. SQS
- **Queue:** `ski-resort-weather-ingestion`
- **Type:** Standard (at-least-once delivery)
- **Purpose:** Decouples queue-driven weather/snow ingestion from synchronous API traffic. External producers publish messages; `SqsIngestionWorker` in the API task polls, deserializes `WeatherIngestionMessage`, persists `SnowCondition` to RDS, then notifies clients via SignalR. **Scheduled** weather is handled separately by `WeatherSyncWorker` calling Open-Meteo (no SQS).
- **Worker config:** `SqsIngestion__QueueUrl` env var, long-polling (10s wait time), up to 10 messages per receive.

---

### 10. AWS Secrets Manager
- **Secret:** `ski-resort/rds-password`
- **Purpose:** Stores the RDS master password (full connection string as a key/value pair). The ECS task definition references the secret ARN so the plain password is never stored in the task definition JSON or environment variables in plain text.
- **IAM:** `ecsTaskExecutionRole` has `secretsmanager:GetSecretValue` permission on this secret. The secret is fetched by the ECS agent **before** the container starts.
- **VPC Endpoint:** `ski-resort-endpoint` (Interface type, `com.amazonaws.us-east-1.secretsmanager`) ensures the secret fetch stays within the VPC without going over the public internet.

---

### 11. CloudWatch
- **Log groups:**
  - `/ecs/ski-resort-api` — application logs from the .NET API container
  - `/ecs/ski-resort-api-adot` — logs from the ADOT collector sidecar
- **Telemetry:** The API exports OpenTelemetry traces and metrics via OTLP to the ADOT sidecar on `127.0.0.1:4317`. The sidecar forwards them to CloudWatch using the default ECS config (`ecs-default-config.yaml`).
- **Instrumentation:** ASP.NET Core, HttpClient, Entity Framework Core, and runtime metrics are all instrumented via OpenTelemetry SDK packages.

---

### 12. IAM Roles

| Role | Used by | Key permissions |
|------|---------|-----------------|
| `ecsTaskExecutionRole` | ECS agent (pull image, start container) | ECR pull, CloudWatch Logs write, Secrets Manager read |
| `ski-resort-api-task-role` | Running container (AWS SDK calls) | SQS receive/delete on `ski-resort-weather-ingestion`, DynamoDB CRUD on `SkiResortUserFavorites` |

---

### 13. VPC and Networking

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

### Page load (Angular SPA)
```
Browser → CloudFront (edge cache) → S3 bucket → index.html + JS/CSS chunks
```

### API request
```
Browser → API Gateway (CORS check) → ALB → ECS task (port 8080) → RDS PostgreSQL
```

### Real-time update (queue)
```
SQS message → SqsIngestionWorker → RDS (persist SnowCondition) → ResortUpdateNotifier → SignalR hub → Browser
```

### Real-time update (scheduled Open-Meteo)
```
WeatherSyncWorker → Open-Meteo API → RDS (persist) → ResortUpdateNotifier → SignalR hub → Browser
```

### Favorites
```
Browser → API Gateway → ALB → ECS task → DynamoDB (SkiResortUserFavorites)
```

### Observability
```
ECS task → OTLP (127.0.0.1:4317) → adot-collector → CloudWatch Logs/Metrics/Traces
```

### Frontend redeployment
```
ng build → aws s3 sync → aws cloudfront create-invalidation
```

---

## Related diagrams in repo

Machine-readable architecture and sequence diagrams (including a dark-theme PlantUML style): [`component-architecture.puml`](component-architecture.puml), [`sequence-data-ingestion.puml`](sequence-data-ingestion.puml), shared styles in [`plantuml-dark.inc.puml`](plantuml-dark.inc.puml), and exported PNGs in [`diagrams/`](diagrams/). The repository [`README.md`](../README.md) summarizes the stack and links to this file.
