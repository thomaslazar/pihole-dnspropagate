# Enforce Branch Protection Rules
- Type: Task
- Phase: 8
- Prerequisites: PIDP-013
- Status: Planned

## Context
With the branching strategy defined, we need to configure GitHub branch protection so `main` (and optionally `develop`) respects the documented rules: PR-only merges, status checks, and reviews. This ensures releases flow through the intended workflow.

## Work Items
- [ ] Decide exact protection settings (required checks, review counts, linear history) per branch.
- [ ] Evaluate GitHub MCP capabilities for branch protection; apply settings via API if supported, otherwise document the manual GitHub UI steps.
- [ ] Verify protections using test PRs (ensure direct push to `main` is blocked).
- [ ] Record the configuration in `docs/release-process.md` or similar reference.
- [ ] When illustrating the enforcement flow, use Mermaid diagrams for consistency.
- [ ] Ensure documentation clarifies expectations for PRs from forks (status checks, permissions, required reviewers).

## Acceptance Criteria
- [ ] `main` blocks direct pushes and requires the agreed status checks and reviews.
- [ ] `develop` has the agreed level of protection.
- [ ] Documentation exists describing how protections are configured and how to update them.

## Notes
- If using automation (e.g., GitHub CLI), commit reusable scripts under `.github/`.
