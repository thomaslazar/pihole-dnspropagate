# Avoid Unnecessary Secondary Updates
- Type: Story
- Phase: 3
- Prerequisites: PIDP-005
- Status: Completed

## Context
Telemetry shows that syncing without actual DNS/CNAME changes still triggers a Teleporter restore on secondaries, interrupting Pi-hole DNS handling. We need to compare primary vs. secondary data and only POST when differences exist. Additionally, operators want a `sync-now` flag to force a refresh regardless of the diff, and the manual CLI should expose clear help text for all options.

## Work Items
- [x] Detect DNS/CNAME diffs before uploading to each secondary; skip Teleporter POST when no changes are required.
- [x] Add CLI flag (e.g., `--force`) to `sync-now` that overrides the diff check for manual runs.
- [x] Improve manual CLI option parsing/help output so `--help`/`-h` describes available switches (`--dry-run`, `--force`, etc.).
- [x] Update unit/integration tests to cover diff detection, forced uploads, and CLI help output.
- [x] Document the new behaviour in README and docs (include note on potential Pi-hole downtime avoidance).

## Acceptance Criteria
- [x] Scheduler skips secondary updates when their current Teleporter data matches the primary’s desired state.
- [x] `sync-now --force` (or equivalent) uploads regardless of diffs.
- [x] `sync-now --help` (and `-h`) outputs clear instructions for manual options.
- [x] Tests cover the diff-based skip logic, forced uploads, and CLI help text; documentation reflects the change.

## Notes
- Consider caching recent primary hashes to avoid recomputing diffs repeatedly.
- Ensure logs clearly state when a secondary is skipped due to no changes.
