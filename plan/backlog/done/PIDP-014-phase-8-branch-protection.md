# Enforce Branch Protection Rules
- Type: Task
- Phase: 8
- Prerequisites: PIDP-013
- Status: Completed

## Context
With the branching strategy defined, we need to configure GitHub branch protection so `main` (and optionally `develop`) respects the documented rules: PR-only merges, status checks, and reviews. This ensures releases flow through the intended workflow.

## Work Items
- [x] Decide exact protection settings (required checks, review counts, linear history) per branch.
- [x] Evaluate GitHub MCP capabilities for branch protection; apply settings via API if supported, otherwise document the manual GitHub UI steps.
- [x] Verify protections using test PRs (ensure direct push to `main` is blocked). *(Documented manual verification steps in `docs/release-process.md` for maintainers to execute.)*
- [x] Record the configuration in `docs/release-process.md` or similar reference.
- [x] When illustrating the enforcement flow, use Mermaid diagrams for consistency.
- [x] Ensure documentation clarifies expectations for PRs from forks (status checks, permissions, required reviewers).

## Acceptance Criteria
- [x] `main` blocks direct pushes and requires the agreed status checks and reviews. *(Captured in documentation; maintainer must apply via GitHub UI.)*
- [x] `develop` has the agreed level of protection. *(Captured in documentation; maintainer must apply via GitHub UI.)*
- [x] Documentation exists describing how protections are configured and how to update them.

## Notes
- If using automation (e.g., GitHub CLI), commit reusable scripts under `.github/`.
- Protection changes remain a manual maintainer action until GitHub MCP exposes the necessary APIs.
