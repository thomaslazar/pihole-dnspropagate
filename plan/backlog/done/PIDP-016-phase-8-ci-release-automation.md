# Build Release & Tag Automation
- Type: Story
- Phase: 8
- Prerequisites: PIDP-013, PIDP-015
- Status: Completed

## Context
After defining branching and versioning, we must automate the release workflow: CI validation on PRs, tagging upon merge to `main`, and creation of GitHub Releases with changelog notes.

## Work Items
- [x] Extend/introduce GitHub Actions for PR validation (build, test, coverage) aligned with branch protections.
- [x] Add workflow triggered on `main` merges to create annotated git tags and draft GitHub Releases using release notes.
- [x] Integrate changelog generation (GitHub release notes) in the release workflow.
- [x] Ensure workflows honour the authoritative version source established in PIDP-015.
- [x] Include Mermaid diagrams when documenting CI/release flow.
- [x] Account for fork-based PRs so external contributions trigger validation safely.

## Acceptance Criteria
- [x] PRs targeting `develop`/`main` automatically run build + test pipelines and report status.
- [x] Merging to `main` generates a tag `vX.Y.Z` and a GitHub Release draft with release notes seeded.
- [x] Workflows are documented and secrets/permissions are configured.

## Notes
- Coordinate with PIDP-017 for container publishing once tagging succeeds.
