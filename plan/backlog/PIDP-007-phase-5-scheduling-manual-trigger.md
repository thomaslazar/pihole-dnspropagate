# Implement Scheduling & Manual Trigger
- Type: Story
- Phase: 5
- Prerequisites: PIDP-006
- Status: Planned

## Context
We need a lightweight scheduler using Cronos to run syncs at configured intervals and a Spectre.Console CLI command to trigger ad-hoc runs aligned with the same pipeline.

## Work Items
- [ ] Add scheduler service leveraging Cronos expressions with `PeriodicTimer` backoff.
- [ ] Expose `sync-now` CLI command invoking the coordinator directly.
- [ ] Ensure shared code path between scheduled and manual executions.
- [ ] Implement health endpoint and metrics hooks (e.g., last sync status, timestamp).
- [ ] Prevent overlapping sync runs by guarding manual/scheduled triggers.

## Acceptance Criteria
- [ ] Interval-based sync executes according to cron expressions in integration tests.
- [ ] `docker compose run sync-now` triggers an immediate sync using CLI command.
- [ ] `/healthz` endpoint signals service state and exposes last sync metadata.
- [ ] Manual triggers launched during an active sync are deferred or rejected with clear messaging.

## Notes
- Provide sensible defaults when cron expression is omitted (e.g., every 5 minutes).
