# Define Branching & Release Governance
- Type: Story
- Phase: 8
- Prerequisites: None
- Status: Completed

## Context
The repository needs a documented branching model so future releases follow consistent rules. We will formalize `main`, `develop`, and feature/hotfix branch usage, including how they interact during releases. This guidance underpins automation and branch protection.

## Work Items
- [x] Draft branching strategy (text + diagram) based on plan/release-plan.md and validate with maintainer.
- [x] Update `AGENTS.md` and `docs/contributing.md` to describe branch roles, naming conventions, and merge expectations.
- [x] Add guidance on when to create hotfix vs feature branches and how to sync `develop` after a hotfix.
- [x] Capture branch relationships using Mermaid diagrams wherever visual aids are required.
- [x] Include instructions for contributors working from forks (preferred branch prefixes, PR target branch, required checks).

## Acceptance Criteria
- [x] Documentation clearly differentiates `main`, `develop`, feature/bugfix, and hotfix responsibilities.
- [x] Contributors know which branch to target for new work and how release PRs are managed.
- [x] Maintainer approves the branching guidance.

## Notes
- Reference this item from subsequent automation tasks requiring the branching model.
