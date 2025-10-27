# Product Specification: Pi-hole DNS Propagation Service

## Vision
Deliver a lightweight service that keeps custom DNS and CNAME records aligned across multiple Pi-hole installations so network operators can manage these entries in one place without hand-editing secondary nodes.

## Problem Statement
Home lab and small-office operators often run multiple Pi-hole nodes for redundancy, but Pi-hole’s built-in sync features do not copy Local DNS or CNAME overrides. Administrators currently export/import Teleporter archives manually or rely on brittle scripts, leaving clusters misaligned and error-prone.

## Goals & Success Metrics
- Within one command or scheduled run, replicate Local DNS and CNAME entries from a designated primary Pi-hole to any number of secondary nodes.
- First release succeeds if operators can trigger a manual sync and see identical Local DNS and CNAME lists on all targeted Pi-holes.
- Longer term, sustain successful nightly syncs for 30 consecutive days in a pilot environment without manual correction.

## Target Users & Personas
- **Network Hobbyist / Home Lab Admin:** Runs 2–4 Pi-holes for failover, expects simple tools and container-first distribution.
- **Small Office IT Steward:** Needs auditable, repeatable syncs that work inside controlled Docker infrastructure.

## Key User Stories
1. As the operator, I configure the service once with API endpoints and keys so it can authenticate against my Pi-hole cluster.
2. As the operator, I trigger a sync and the system replaces Local DNS and CNAME entries on all secondary Pi-holes with the primary’s values.
3. As the operator, I can review logs or metrics to confirm the sync happened and see which records changed.
4. As the operator, I run the service in Docker Compose and it self-updates records on a configurable interval.

## Functional Requirements
- Support configuration via environment variables (primary URL, primary admin credential, list of secondary URLs with credentials, sync interval, optional dry-run). Accept hashed Pi-hole `WEBPASSWORD` values (same as API token) and store them only in environment variables.
- Offer a manual trigger command accessible through the container (e.g., `dotnet run -- sync-now`) to force a sync outside the scheduler.
- Perform Pi-hole auth handshake per official docs: submit hashed password to establish a session cookie and CSRF token before calling Teleporter.
- Fetch the Teleporter export from the primary Pi-hole `/teleporter` endpoint once authenticated.
- Parse Teleporter payload to isolate Local DNS and CNAME configuration.
- Fetch Teleporter data from each secondary Pi-hole, replace their Local DNS and CNAME sections with the primary data, and post the modified archive back to `/teleporter`.
- Execute sync on startup and thereafter on a configurable fixed interval (default 5 minutes).
- Provide idempotent behavior: re-running when records already match should result in no changes beyond verification.
- Emit structured logs summarizing records copied, nodes updated, or errors encountered.
- Provide a simple health endpoint or exit code suitable for container monitoring.
- Enable future selective sync mode (merge without deletions) while default remains full replacement.

## Non-Functional Requirements
- Implement in .NET 9 (C#) with async I/O.
- Provide official Docker image and `docker-compose.yaml` example using minimal runtime footprint.
- Supply `.devcontainer/` configuration for VS Code with necessary SDKs, debugger, and test tools.
- Ensure network operations are resilient: retry transient failures, time out gracefully, and surface error context.
- Emit structured JSON logs suitable for container aggregation with configurable log levels.
- Maintain test coverage of at least 80% for sync orchestration and Teleporter parsing logic.
- Design configuration binding with `IOptionsMonitor` to support future hot-reload scenarios.

## Release Milestones
1. **MVP Sync (v0.1.0):** Environment-driven configuration with hashed admin credentials, manual trigger via CLI, full replacement sync, basic logging.
2. **Automation & Observability (v0.2.0):** Interval scheduler, health endpoint, metrics counters, error handling improvements.
3. **Selective Sync & Ops (v0.3.0):** Add merge-only mode that honors existing secondary overrides, richer authentication options, user-facing status dashboard (optional).

## Telemetry & Observability
- Container logs include sync start/finish, nodes targeted, counts of records replaced, and failure details.
- Optional metrics endpoint (e.g., Prometheus) scoped for future milestone, but logging must support troubleshooting from day one.

## Dependencies & Integrations
- Pi-hole API `/teleporter` endpoint (primary + each secondary).
- Docker runtime for deployment; Docker Compose for orchestration.
- GitHub Container Registry (GHCR) or similar repository for distributing images (final decision pending).

## Risks & Mitigations
- **Teleporter Authentication:** Session-based flow requires maintaining cookies and CSRF token; encapsulate in dedicated client with retry and re-auth logic.
- **Large Config Payloads:** Teleporter archives may grow; implement streaming to avoid high memory usage.
- **Partial Failures:** If a secondary Pi-hole fails mid-sync, system must log and continue other nodes, then retry on next cycle.

## Open Questions
- None currently.
