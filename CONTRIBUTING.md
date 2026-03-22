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
  - **SignalR across multiple API tasks (fixes 404 “No Connection with that ID”)**  
    When more than one ECS task runs the API, use a **Redis backplane** and set **`ConnectionStrings__Redis`**.  
    Step-by-step AWS setup: **[ElastiCache Redis and ECS](#elastiache-redis-and-ecs-signalr-backplane)** below.  
    If unset, SignalR stays **in-memory** (OK for `dotnet run` or a **single** task).

- **Front end (when present)**
  - Use environment files (`.env`, `.env.local`, Angular env files) with:
    - Example values checked in via `*.example` or `*.sample`.
    - Actual values in files ignored by Git.

- **Patterns to follow**
  - For any new secret/config:
    1. Add a **placeholder** or example to `README.md` or an `*.example` config file.
    2. Read the value from **environment variables** (or `IConfiguration` for .NET).
    3. Ensure the real file (`.env`, `secrets.json`, etc.) is **ignored by Git**.

### ElastiCache Redis and ECS (SignalR backplane)

Use this when the API runs **more than one** ECS task behind a load balancer and SignalR must work across tasks.

#### 1. Network (same VPC as ECS)

- Use the **same VPC** as your ECS service (and the same **Region**).
- ElastiCache must live in **subnets** that can be reached from your tasks. Prefer **private subnets** (no public IP on cache nodes).
- Create or reuse a **cache subnet group** (ElastiCache console → *Subnet groups* → *Create*): add **at least two** subnets in **different Availability Zones** (AWS requirement for replication groups).

#### 2. Security groups

1. Identify the **security group attached to your ECS tasks** (the ENIs Fargate/EC2 uses)—call this **`ecs-tasks-sg`**.
2. Create **`elasticache-redis-sg`** (or reuse a dedicated SG):
   - **Inbound rules** (both may be required for **ElastiCache Serverless** and for **StackExchange.Redis**, which opens a separate **Subscription** connection):
     - **Custom TCP** port **6379**, **Source** = **`ecs-tasks-sg`**
     - **Custom TCP** port **6380**, **Source** = **`ecs-tasks-sg`**
   - If you only open **6379**, you may see `UnableToConnect on ...:6380/Subscription` and SignalR can fail across tasks.
   - **Outbound:** default (usually all) is fine.

If you use **different SGs** for ALB vs tasks, the source for Redis must be the **task** SG, because the **container** opens the outbound connection to Redis.

#### 3. Create the Redis cluster (ElastiCache)

Console: **ElastiCache** → **Redis OSS caches** → **Create** (wording may vary slightly).

Suggested options for a first setup:

| Setting | Typical choice |
|--------|----------------|
| Engine | Redis **7.x** (or current default) |
| Cluster mode | **Disabled** (single shard) — simpler for SignalR backplane |
| Node type | e.g. `cache.t4g.micro` / `cache.t3.micro` for non-prod |
| Subnet group | The private subnet group from step 1 |
| Security | Security groups → attach **`elasticache-redis-sg`** |
| Encryption in transit | **Enabled** for production (use `ssl=true` in the app string) |
| Encryption at rest | Optional (recommended in prod) |
| Auth | Optional **Redis AUTH** / user—if enabled, add password to the connection string per AWS docs |

After creation, copy the **Primary endpoint** (hostname), e.g. `master.something.xxxxx.use1.cache.amazonaws.com`.

#### 4. Connection string for .NET (`StackExchange.Redis`)

The API reads **`ConnectionStrings__Redis`** (env: `ConnectionStrings__Redis`).

Examples (adjust hostname; **port is usually 6379** even with TLS on ElastiCache):

```text
master.mycluster.abc123.use1.cache.amazonaws.com:6379,ssl=true,abortConnect=false
```

- **`ssl=true`** — required when **encryption in transit** is on.
- **`abortConnect=false`** — lets the app start if Redis is briefly unreachable at startup (still log/retry in ops).
- If the console shows a **different port** or **Configuration endpoint** (cluster mode enabled), use what AWS documents for your cluster type.
- If you enabled **AUTH**, append the password (see [StackExchange.Redis configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration) and AWS ElastiCache auth docs).

#### 5. ECS task definition

1. Open **Task definitions** → your API task → **Create new revision**.
2. Under the **API container** → **Environment variables** (or **Secrets**):
   - **Name:** `ConnectionStrings__Redis`  
   - **Value:** the full connection string from step 4.  
   For secrets, store the string in **Secrets Manager** or **SSM Parameter Store** and reference it in the task definition instead of plain text.
3. **Save** and **Update** the ECS **service** to use the new revision (force new deployment).

The task execution role does **not** need ElastiCache permissions for a normal Redis password connection—only network reachability and the connection string.

#### 6. Verify

- From a task in the same VPC (e.g. **ECS Exec** into a debug sidecar or a bastion in the VPC): `redis-cli` with TLS flags, or test TCP connectivity to `primary-endpoint:6379`.
- If connections time out: recheck **security groups** (inbound on Redis SG from **task** SG), **subnets/route tables**, and **NACLs**.
- Deploy the API image that includes **`AddStackExchangeRedis`** (see `Program.cs`).

#### 7. Cost / ops notes

- You pay for **node hours** + optional backup; a single small node is usually enough for SignalR metadata traffic (not your Postgres data).
- **Redis is not a replacement for Postgres**—it is only the SignalR backplane here.

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

