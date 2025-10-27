# Implement Sync Coordinator
- Type: Story
- Phase: 4
- Prerequisites: PIDP-005
- Status: Planned

## Context
The coordinator orchestrates fetching primary `pihole.toml` data from the Teleporter zip, updating each secondary’s TOML arrays, and invoking the Teleporter client while tracking outcomes.

## Work Items
- [ ] Compose pipeline to download primary archive, extract records, and prepare replacement payloads.
- [ ] Iterate over secondary instances, applying replacements and handling partial failures gracefully.
- [ ] Surface structured JSON logs summarizing operations per node, including before/after record counts, and support dry-run mode.
- [ ] Gracefully handle secondary instances missing `pihole.toml` and log the condition.
- [ ] Write unit tests exercising dry-run behavior and log formatting.

## Acceptance Criteria
- [ ] Coordinated sync succeeds across multiple secondaries in integration tests.
- [ ] Failures for one node do not prevent others from syncing.
- [ ] Dry-run mode outputs intended changes without applying them.
- [ ] Logs provide per-node before/after counts for hosts and CNAME records.

## Notes
- Include instrumentation hooks for future metrics support.
