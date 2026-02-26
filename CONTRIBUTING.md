# Contributing to SkiResortConditionsDashboard

This document describes how we work on this repo: code review, branching, and how to handle environment variables and secrets.

## 1. Code review process (KEY)

- **All changes go through pull requests**
  - No direct commits to `main` (or the primary default branch).
  - Every feature or fix should be in a short‑lived feature branch.

- **At least one approving review required**
  - Another contributor must approve the PR before it is merged.
  - Reviewers check:
    - Correctness and clarity of the implementation.
    - Tests added or updated where it makes sense.
    - No secrets or sensitive data added.
    - Public APIs and contracts (endpoints, DTOs) documented and stable.

- **Small, focused pull requests**
  - Prefer smaller PRs that are easy to understand and revert.
  - Keep unrelated refactors separate from feature changes.

- **Pre‑merge checklist**
  - `dotnet build` succeeds.
  - `dotnet test` (when tests exist) passes.
  - API still starts locally: `dotnet run --project backend/SkiResort.Api/SkiResort.Api.csproj`.
  - Any new configuration or environment variables are documented in `README.md`.

## 2. Branching and commit practices

- **Branch naming**
  - Use descriptive names such as:
    - `feature/realtime-favorites`
    - `fix/resorts-pagination`
    - `chore/ci-setup`

- **Commit messages**
  - Use clear, action‑oriented messages:
    - `Add SQS ingestion worker`
    - `Fix keyset pagination for run history`
    - `Document local Postgres setup`
  - Group related changes into the same commit; avoid “mixed bag” commits.

## 3. Environment variables and secrets (KEY)

Never commit real secrets or production credentials to this repository.

- **Back end (.NET)**
  - The API reads the Postgres connection string from configuration:
    - `ConnectionStrings__Postgres` environment variable (recommended), or
    - `ConnectionStrings:Postgres` in `appsettings.*.json` (for local only, with non‑sensitive values).
  - AWS SDK (for SQS and DynamoDB) uses:
    - Standard AWS mechanisms (`AWS_PROFILE`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `~/.aws/credentials`, etc.).
    - The region is provided via configuration (`AWS:Region`) or falls back to a safe default.
  - **Do not** commit real AWS keys, tokens, or private endpoints to `appsettings.*.json`.

- **Front end (when present)**
  - Use environment files (`.env`, `.env.local`, Angular env files) with:
    - Example values checked in via `*.example` or `*.sample`.
    - Actual values in files ignored by Git.

- **Patterns to follow**
  - For any new secret/config:
    1. Add a **placeholder** or example to `README.md` or an `*.example` config file.
    2. Read the value from **environment variables** (or `IConfiguration` for .NET).
    3. Ensure the real file (`.env`, `secrets.json`, etc.) is **ignored by Git**.

## 4. Contribution workflow with Git

1. **Create a branch**
   ```bash
   git checkout -b feature/my-change
   ```

2. **Make and test your changes**
   ```bash
   dotnet build
   dotnet test
   dotnet run --project backend/SkiResort.Api/SkiResort.Api.csproj
   ```

3. **Commit**
   ```bash
   git add .
   git commit -m "Describe the change clearly"
   ```

4. **Push and open a PR**
   ```bash
   git push -u origin feature/my-change
   ```
   - Open a pull request in your Git hosting service.
   - Fill in the PR template (see `.github/pull_request_template.md`).
   - Request at least one reviewer.

5. **Address review feedback**
   - Update code, push additional commits.
   - Keep the PR up to date with `main` if necessary (via `git merge` or `git rebase` depending on your policy).

6. **Merge**
   - Once CI is green and the PR is approved, squash or merge according to the repository’s policy.

