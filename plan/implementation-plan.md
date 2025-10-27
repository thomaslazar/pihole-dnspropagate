# Implementation Plan: Pi-hole DNS Propagation Service

## Phase 0 – Project Scaffolding
- Initialize `.devcontainer/` with .NET 9 SDK, Docker CLI, latest Codex CLI, GitKraken CLI (`gk`), and supporting tools (curl, unzip). Install VS Code extensions for .NET development plus:
  - `codezombiech.gitignore`
  - `dbaeumer.vscode-eslint`
  - `donjayamanne.githistory`
  - `eamodio.gitlens`
  - `fabiospampinato.vscode-diff`
  - `github.copilot`
  - `github.copilot-chat`
  - `github.vscode-pull-request-github`
  - `mhutchie.git-graph`
  - `openai.chatgpt`
- Configure `devcontainer.json` with `containerEnv.CODEX_HOME` pointing to `${containerWorkspaceFolder}/.codex`.
- Create `.codex/config.toml` with default settings (no secrets) as provided:
  ```
  model = "gpt-5-codex"
  model_reasoning_effort = "medium"

  [mcp_servers.context7]
  command = "npx"
  args = ["-y", "@upstash/context7-mcp", "--api-key", "YOUR_API_KEY"]

  [mcp_servers.github]
  url = "https://api.githubcopilot.com/mcp/"
  bearer_token_env_var = "GITHUB_MCP_PAT"

  [mcp_servers.gitkraken]
  command = "gk"
  args = ["mcp"]
  ```
- Update `.gitignore` to exclude `.codex/` while committing `config.toml` template without keys.
- Create solution layout: `src/PiholeDnsPropagate`, `src/PiholeDnsPropagate.Worker`, `tests/PiholeDnsPropagate.Tests`.
- Add shared `Directory.Build.props` and `global.json` pinning SDK version.
- Draft `docker-compose.pihole-dev.yml` that spins up one primary and two secondary Pi-hole containers on an isolated network with distinct volumes and hashed admin credentials for local testing.

## Recommended Third-Party Libraries
- **Flurl.Http** – fluent HTTP client that simplifies authenticated requests and file uploads/downloading without manual `HttpClient` plumbing.
- **Polly** – resilience and transient-fault handling (retries, circuit breakers) for Pi-hole API calls via `IHttpClientFactory`.
- **SharpCompress** – supports reading/writing ZIP/TAR archives used by Pi-hole Teleporter exports without manual stream handling.
- **Tomlyn** – .NET TOML parser/serializer for reading and updating `pihole.toml` host and CNAME records.
- **Cronos** – lightweight cron expression evaluator to drive interval scheduling without Quartz overhead.
- **Spectre.Console.Cli** – structured CLI command handling to expose a `sync-now` command with rich help text when running in containers.
- **Serilog + Serilog.Sinks.Console** – structured logging pipeline offering JSON or ANSI output suitable for container logs and future sink extensions.
- **FluentValidation** – declarative validation for configuration options to provide clear startup errors.
- **DotNet.Testcontainers** – orchestrate disposable Pi-hole containers during integration tests without static scripts.
## Phase 1 – Configuration & Hosting
- Implement options classes binding environment variables (primary endpoint/hashed credential, secondary endpoints/credentials, interval, dry-run).
- Validate configuration with FluentValidation to surface actionable startup diagnostics and leverage `IOptionsMonitor` for potential reload support.
- Build a generic host with worker service template to manage scheduled execution.

## Phase 2 – Teleporter Client
- Implement Flurl.Http clients registered via `IHttpClientFactory`, wrapped with Polly policies for timeouts and retries.
- Implement authentication workflow per Pi-hole docs: submit hashed web password to establish session cookies and CSRF token, refreshing on 401/403.
- Create `ITeleporterClient` abstraction with methods: `DownloadArchiveAsync`, `UploadArchiveAsync`.
- Persist and reuse cookies between requests, clearing on auth failure.
- Write unit tests using mocked handlers to cover success, failure, re-auth, and retry scenarios.

## Phase 3 – Teleporter Processing
- Decode Teleporter archive (zip) using SharpCompress. Extract `pihole/pihole.toml` while leaving other files untouched.
- Parse `pihole.toml` with Tomlyn to read `[dns].hosts` and `cnameRecords` arrays representing Local DNS and CNAME entries.
- Replace secondary `[dns].hosts` and `cnameRecords` arrays with primary data while preserving unrelated sections and formatting, leaving all other files as-is.
- Normalize line endings and encoding (UTF-8) to avoid inconsistent diffs across platforms; keep original TOML ordering when possible.
- Provide fixture builders that generate Teleporter zips with representative `pihole.toml` content for unit tests without requiring Pi-hole containers and validate structure parity.

## Phase 4 – Sync Orchestrator
- Implement `SyncCoordinator` that:
  1. Fetches primary archive and extracts target data.
  2. Iterates through secondary nodes, replacing Local DNS/CNAME sections.
  3. Uploads reconstructed archives and tracks results per node.
- Add structured logging summarizing operations and outcomes using Serilog.
- Provide dry-run mode that logs intended changes without uploading.

## Phase 5 – Scheduling, Triggers & Health
- Use Cronos to evaluate cron-style expressions or fixed intervals, driving a lightweight scheduler built on `PeriodicTimer`.
- Implement manual trigger command/entrypoint via Spectre.Console.Cli so `docker compose run sync-now` executes the same sync pipeline.
- Expose minimal health endpoint or readiness probe using Kestrel (optional simple HTTP listener).
- Integrate metrics/logging hooks for future observability upgrades.

## Phase 6 – Testing & Quality Gates
- Expand test suite with integration-style tests using in-memory archives.
- Achieve target coverage for parsing and orchestration layers; document test commands.
- Configure GitHub Actions (or placeholder) for CI with `dotnet build/test` on .NET 9.
- Add integration tests leveraging DotNet.Testcontainers to provision disposable Pi-hole instances, seeded via Teleporter exports, and verify end-to-end sync.
- Provide unit-level tests exercising archive manipulation via fixture builders to keep feedback loops fast.

## Phase 7 – Packaging & Docs
- Produce `docker-compose.yaml` example and multi-stage Dockerfile.
- Document configuration in `docs/configuration.md` and update README usage instructions.
- Publish `.devcontainer` documentation for contributors and verify containerized development workflow.
- Provide `.env.dev` template (gitignored) and guidance for managing hashed credentials securely during local testing.

## Acceptance Checklist
- Manual sync run copies Local DNS and CNAME entries across at least two secondary Pi-holes.
- Logs show per-node status and error traces when failures occur.
- JSON-formatted container logs verified for readability and integration with `docker compose logs`.
- Container image builds cleanly with multi-stage Docker build.
- Tests pass under `dotnet test` within devcontainer environment.
- Selective sync mode captured as backlog item with design notes for Phase 8+.
- Local dev compose file successfully starts three Pi-hole containers and supports manual sync testing.
