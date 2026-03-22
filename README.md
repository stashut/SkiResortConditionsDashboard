# Ski Resort Conditions Dashboard

A production-style, low-traffic **ski resort conditions** dashboard: a .NET 8 Web API with Entity Framework Core and Dapper, PostgreSQL on Amazon RDS, favorites in DynamoDB, weather ingestion via Amazon SQS, real-time updates through SignalR, and an **Angular 17** single-page application with Angular Material.

## Architecture (summary)

- **Frontend**: Angular SPA under `frontend/ski-resort-dashboard` (Material UI, `@microsoft/signalr` for live updates).
- **Backend**: ASP.NET Core API in `backend/SkiResort.Api` — REST endpoints, SignalR hub at `/hubs/resort-conditions`, session-backed settings, background workers.
- **Data**: PostgreSQL for resorts, snow conditions, lift/run snapshots; DynamoDB for per-user favorites when not using the in-memory dev repository.
- **Messaging**: SQS queue consumed by `SqsIngestionWorker`; optional `WeatherSyncWorker` calls **Open-Meteo** on a schedule and persists to PostgreSQL.
- **Observability**: OpenTelemetry traces and metrics in `Program.cs`, with meters and activities in `ObservabilityConstants`, exported via OTLP (for example to the AWS Distro for OpenTelemetry sidecar → CloudWatch in AWS).

**Diagrams (PlantUML)** in `docs/`:

| File | Purpose |
|------|---------|
| [`docs/sequence-data-ingestion.puml`](docs/sequence-data-ingestion.puml) | SQS ingestion and scheduled Open-Meteo sync → RDS → SignalR → browser |
| [`docs/component-architecture.puml`](docs/component-architecture.puml) | Angular, API Gateway, ALB, ECS, RDS, DynamoDB, SQS, OTel, CloudFront/S3 |

Render locally with a [PlantUML](https://plantuml.com/) extension or CLI, or paste into any PlantUML-compatible renderer.

**AWS deployment (console resources, security groups, URLs)** is documented in [`docs/aws-architecture.md`](docs/aws-architecture.md).

---

## Local development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS) and npm — for the Angular app
- [Docker](https://www.docker.com/) — optional but recommended for PostgreSQL via Compose

### Backend

From the repository root:

```bash
dotnet build
```

Run the API (uses `appsettings.Development.json` for PostgreSQL unless overridden):

```bash
dotnet run --project backend/SkiResort.Api/SkiResort.Api.csproj
```

**Database with Docker Compose** (Postgres + API container):

```bash
docker compose up --build
```

The API is exposed on **http://localhost:5000** (mapped to container port 8080). Ensure migrations/schema match your environment; in Development the app may call `EnsureCreated` and seed data — see `backend/SkiResort.Api/DevSeeder.cs`.

### Frontend

```bash
cd frontend/ski-resort-dashboard
npm install
npm start
```

Configure the SPA to call your API base URL (environment / proxy as set up in the Angular project).

### Configuration and secrets

- **PostgreSQL**: `ConnectionStrings:Postgres` in `backend/SkiResort.Api/appsettings.Development.json`, or override with `ConnectionStrings__Postgres`.
- **AWS (SQS, DynamoDB)**: default region from `AWS:Region` or `us-east-1`; credentials via the standard AWS SDK chain (`AWS_PROFILE`, environment variables, etc.).
- **SQS ingestion**: set `SqsIngestion:QueueUrl` (or env `SqsIngestion__QueueUrl`) to enable `SqsIngestionWorker`; if empty, the worker logs and idles.
- **Favorites**: `Favorites:UseInMemoryRepository` defaults to `true` in Development; set to `false` and configure DynamoDB for AWS-like behavior locally.
- **SignalR scale-out**: optional Redis backplane via `ConnectionStrings:Redis` or `SignalR:RedisConnectionString` for multiple API instances.
- Do **not** commit real secrets. Use environment variables or a secret manager.

### Contract tests (Pact)

- **Consumer-style pact generation** (writes JSON under `pacts/`): `backend/SkiResort.Tests/Contracts/ResortsConsumerPactTests.cs`
- **Provider verification** (reads pact file; requires running API when `PACT_PROVIDER_BASE_URL` is set): `backend/Pact.Provider.Tests/Contracts/ResortsProviderPactTests.cs`

---

## Running in AWS (high level)

1. Build and push the API image from `backend/SkiResort.Api/Dockerfile` to Amazon ECR.
2. Run the container on **Amazon ECS Fargate** behind an **Application Load Balancer**; expose **Amazon API Gateway HTTP API** to the internet with routes such as `ANY /api/{proxy+}`.
3. Provision **Amazon RDS for PostgreSQL**, **DynamoDB** table `SkiResortUserFavorites`, and **SQS** queue `ski-resort-weather-ingestion`; grant the task IAM role SQS and DynamoDB permissions.
4. Host the Angular build on **S3** with **CloudFront**; point the SPA at the API Gateway base URL.
5. Optionally run the **AWS Distro for OpenTelemetry** collector as a sidecar and configure OTLP export to **CloudWatch**.

Step-by-step resource names, networking, and CORS notes are in [`docs/aws-architecture.md`](docs/aws-architecture.md).

---

## Evaluation criteria — evidence mapping

Use this table to trace each criterion to concrete artifacts in the repository.

| # | Criterion (summary) | Where to look in this repo |
|---|---------------------|----------------------------|
| **1** | Cloud API router (e.g. API Gateway) | [`docs/aws-architecture.md`](docs/aws-architecture.md) (HTTP API routes, integration to ALB); [`docs/component-architecture.puml`](docs/component-architecture.puml) |
| **2** | API managed in cloud (e.g. ECS Fargate) | [`backend/SkiResort.Api/Dockerfile`](backend/SkiResort.Api/Dockerfile); [`docker-compose.yml`](docker-compose.yml) (`api` service); [`docs/aws-architecture.md`](docs/aws-architecture.md) (ECS service, task, ALB) |
| **3** | Message queuing (SQS) | [`backend/SkiResort.Api/Workers/SqsIngestionWorker.cs`](backend/SkiResort.Api/Workers/SqsIngestionWorker.cs); [`backend/SkiResort.Api/Options/SqsIngestionOptions.cs`](backend/SkiResort.Api/Options/SqsIngestionOptions.cs); [`backend/SkiResort.Api/Program.cs`](backend/SkiResort.Api/Program.cs) (`AddAWSService<IAmazonSQS>()`, hosted service); [`docs/sequence-data-ingestion.puml`](docs/sequence-data-ingestion.puml) |
| **4** | Cloud sync / NoSQL (DynamoDB) | [`backend/SkiResort.Infrastructure/Favorites/UserFavoritesRepository.cs`](backend/SkiResort.Infrastructure/Favorites/UserFavoritesRepository.cs); [`backend/SkiResort.Api/Controllers/FavoritesController.cs`](backend/SkiResort.Api/Controllers/FavoritesController.cs); [`backend/SkiResort.Api/Program.cs`](backend/SkiResort.Api/Program.cs) (DynamoDB registration); Angular: `frontend/ski-resort-dashboard/src/app/core/services/favorites.service.ts` and favorites feature |
| **5** | **KEY** — RDS PostgreSQL | [`backend/SkiResort.Infrastructure/Data/SkiResortDbContext.cs`](backend/SkiResort.Infrastructure/Data/SkiResortDbContext.cs); [`backend/SkiResort.Domain/Entities/`](backend/SkiResort.Domain/Entities/); [`backend/SkiResort.Infrastructure/Migrations/`](backend/SkiResort.Infrastructure/Migrations/); [`docker-compose.yml`](docker-compose.yml) (`db` service) |
| **6** | **KEY** — Containers / ECS | [`backend/SkiResort.Api/Dockerfile`](backend/SkiResort.Api/Dockerfile); [`docker-compose.yml`](docker-compose.yml); ECS task and cluster details in [`docs/aws-architecture.md`](docs/aws-architecture.md) |
| **7** | Pact contract tests | [`backend/SkiResort.Tests/Contracts/ResortsConsumerPactTests.cs`](backend/SkiResort.Tests/Contracts/ResortsConsumerPactTests.cs); [`backend/Pact.Provider.Tests/Contracts/ResortsProviderPactTests.cs`](backend/Pact.Provider.Tests/Contracts/ResortsProviderPactTests.cs); generated pact under `pacts/` (after running consumer tests) |
| **8** | OpenTelemetry → CloudWatch | [`backend/SkiResort.Api/Program.cs`](backend/SkiResort.Api/Program.cs) (`AddOpenTelemetry`, OTLP exporter); [`backend/SkiResort.Api/Observability/ObservabilityConstants.cs`](backend/SkiResort.Api/Observability/ObservabilityConstants.cs); ADOT / CloudWatch wiring in [`docs/aws-architecture.md`](docs/aws-architecture.md) |
| **9** | SignalR real-time | [`backend/SkiResort.Api/Hubs/ResortConditionsHub.cs`](backend/SkiResort.Api/Hubs/ResortConditionsHub.cs); [`backend/SkiResort.Api/Realtime/ResortUpdateNotifier.cs`](backend/SkiResort.Api/Realtime/ResortUpdateNotifier.cs); [`backend/SkiResort.Api/Program.cs`](backend/SkiResort.Api/Program.cs) (`MapHub`); [`frontend/ski-resort-dashboard/src/app/core/services/signalr-resort-updates.service.ts`](frontend/ski-resort-dashboard/src/app/core/services/signalr-resort-updates.service.ts) |
| **10** | Sessions | [`backend/SkiResort.Api/Program.cs`](backend/SkiResort.Api/Program.cs) (`AddSession`, `UseSession`); [`backend/SkiResort.Api/Controllers/SettingsController.cs`](backend/SkiResort.Api/Controllers/SettingsController.cs); Angular settings feature and `settings.service.ts` |
| **11** | Large / efficient data access | Keyset pagination and conditions: [`backend/SkiResort.Api/Controllers/ResortsController.cs`](backend/SkiResort.Api/Controllers/ResortsController.cs) (`GET .../conditions`); streaming: `GET .../snow-history/stream` (`IAsyncEnumerable`); Dapper report: [`backend/SkiResort.Infrastructure/Reports/SnowComparisonReportRepository.cs`](backend/SkiResort.Infrastructure/Reports/SnowComparisonReportRepository.cs); [`backend/SkiResort.Api/Controllers/ReportsController.cs`](backend/SkiResort.Api/Controllers/ReportsController.cs) (`GET /api/reports/snow-comparison`) |
| **12** | UML / PlantUML | [`docs/sequence-data-ingestion.puml`](docs/sequence-data-ingestion.puml); [`docs/component-architecture.puml`](docs/component-architecture.puml) |

---

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for the code review workflow and conventions.
