# SkiResortConditionsDashboard

A real-time ski resort snow and lift status aggregator.

## Development

- Build solution:
  ```bash
  dotnet build
  ```
- Run API:
  ```bash
  dotnet run --project backend/SkiResort.Api/SkiResort.Api.csproj
  ```

### Configuration, environment variables, and secrets

- **Postgres connection string**
  - Default (local) connection is configured under `ConnectionStrings:Postgres` in `backend/SkiResort.Api/appsettings.Development.json`.
  - You can override it via environment variable:
    - `ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=ski_resort;Username=ski;Password=ski_dev"`
- **AWS configuration (SQS, DynamoDB)**
  - Region is set via configuration (`AWS:Region`) or falls back to `us-east-1`.
  - Credentials are resolved by the AWS SDK using standard mechanisms (`AWS_PROFILE`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `~/.aws/credentials`, etc.).
- **Secrets**
  - Do **not** commit real secrets (passwords, tokens, AWS keys).
  - Use environment variables or your local secret store instead.

See `CONTRIBUTING.md` for the full code review process and contribution workflow.
