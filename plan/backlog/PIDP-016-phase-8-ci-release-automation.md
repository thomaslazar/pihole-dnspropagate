# Build Release & Tag Automation
- Type: Story
- Phase: 8
- Prerequisites: PIDP-013, PIDP-015
- Status: Planned

## Context
After defining branching and versioning, we must automate the release workflow: CI validation on PRs, tagging upon merge to `main`, and creation of GitHub Releases with changelog notes.

## Work Items
- [ ] Extend/introduce GitHub Actions for PR validation (build, test, coverage) aligned with branch protections.
- [ ] Add workflow triggered on `main` merges (or tags) to create annotated git tags and draft GitHub Releases using release notes.
- [ ] Integrate changelog generation (e.g., based on commits or curated notes).
- [ ] Ensure workflows honour the authoritative version source established in PIDP-015.
- [ ] Include Mermaid diagrams when documenting CI/release flow.
- [ ] Account for fork-based PRs (use `pull_request_target` or permissions as appropriate) so external contributions trigger validation safely.

## Acceptance Criteria
- [ ] PRs targeting `develop`/`main` automatically run build + test pipelines and report status.
- [ ] Merging to `main` generates a tag `vX.Y.Z` and a GitHub Release draft with release notes seeded.
- [ ] Workflows are documented and secrets/permissions are configured.

## Notes
- Coordinate with PIDP-017 for container publishing once tagging succeeds.
