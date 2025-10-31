# Retire Legacy CI Workflow
- Type: Task
- Phase: 8
- Prerequisites: PIDP-016
- Status: Completed

## Context
After introducing the dedicated `PR Validation` and `Release` workflows, the legacy `.github/workflows/ci.yml` no longer serves a unique purpose. Removing it avoids redundant runs and clarifies branch protection requirements.

## Work Items
- [x] Delete `.github/workflows/ci.yml` and any references to the legacy workflow.
- [x] Update documentation to reflect the streamlined workflow set (PR Validation, Release, Manual Image Build).
- [x] Verify branch protection guidance still points contributors to the correct status check (`PR Validation / build`).
- [x] Run regression tests to ensure no build logic depended on the removed workflow.

## Acceptance Criteria
- [x] Repository no longer contains `.github/workflows/ci.yml`.
- [x] Docs mention only the active workflows and instructions remain accurate.
- [x] `dotnet test` passes locally.

## Notes
- Coordinate with maintainers if branch protection rules need manual adjustment once the redundant job disappears.
