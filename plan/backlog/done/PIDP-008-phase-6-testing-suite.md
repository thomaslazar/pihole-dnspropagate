# Build Testing & Quality Suite
- Type: Story
- Phase: 6
- Prerequisites: PIDP-007
- Status: Completed

## Context
Establish comprehensive testing for orchestration, archive processing, and configuration to meet coverage goals and CI integration.

## Work Items
- [x] Implement unit tests covering configuration binding, validation, and coordinator logic.
- [x] Build helper fixtures and in-memory Teleporter archives for fast testing.
- [x] Add regression tests asserting only `pihole.toml` differs between original and synced Teleporter zips.
- [x] Add Testcontainers-based integration tests spinning up Pi-hole instances and clean up volumes/networks after runs.
- [x] Add coverage reporting (`dotnet test --collect:"XPlat Code Coverage"`).
- [x] Configure CI pipeline template (GitHub Actions) running build, test, coverage.

## Acceptance Criteria
- [x] Test suite achieves ≥80% coverage on critical projects.
- [x] CI pipeline executes successfully with status badges ready for README.
- [x] Integration tests reliably provision and tear down Pi-hole containers without manual steps and leave no residual Docker resources.
- [x] Regression tests confirm non-TOML files remain unchanged post-sync.

## Notes
- Consider using Coverlet collector settings for deterministic coverage output.
- Integration suite auto-skips when Docker is unavailable; CI environments must provide Docker access for full coverage.
