# Implement Sync Coordinator
- Type: Story
- Phase: 4
- Prerequisites: PIDP-005
- Status: Completed

## Context
The coordinator orchestrates fetching primary `pihole.toml` data from the Teleporter zip, updating each secondary’s TOML arrays, and invoking the Teleporter client while tracking outcomes.

## Work Items
- [x] Compose pipeline to download primary archive, extract records, and prepare replacement payloads.
- [x] Iterate over secondary instances, applying replacements and handling partial failures gracefully.
- [x] Surface structured JSON logs summarizing operations per node, including before/after record counts, and support dry-run mode.
- [x] Gracefully handle secondary instances missing `pihole.toml` and log the condition.
- [x] Write unit tests exercising dry-run behavior and log formatting.

## Acceptance Criteria
- [x] Coordinated sync succeeds across multiple secondaries in integration tests.
- [x] Failures for one node do not prevent others from syncing.
- [x] Dry-run mode outputs intended changes without applying them.
- [x] Logs provide per-node before/after counts for hosts and CNAME records.

## Notes
- Include instrumentation hooks for future metrics support.
- Structured logging uses pre-defined `LoggerMessage` delegates and JSON payload snapshots; integration test `SyncCoordinatorAppliesChangesAcrossSecondaries` verifies sandbox end-to-end behavior.
