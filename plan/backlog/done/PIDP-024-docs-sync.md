# Align README and Configuration Docs
- Type: Task
- Prerequisites: PIDP-021
- Status: Completed

## Context
Documentation mentions diff-based updates and external networks that no longer match the current implementation. The README should reflect change detection via Teleporter archive replacement, and the prod compose file now defines its own network. This task keeps public docs consistent with the actual behaviour.

## Work Items
- [x] Rename the README feature bullet to "Change-aware sync" (or similar) to avoid confusion with roadmap items.
- [x] Update the "How It Works" section to clarify that the Teleporter archive is replaced wholesale, not selectively updated.
- [x] Mention in README (and doc snippets if helpful) that change detection determines whether we push an archive before overwriting.
- [x] Update `docs/configuration.md` to match the current prod compose network definition (no external dependency).

## Acceptance Criteria
- [x] README feature list and “How It Works” accurately reflect the implemented behaviour.
- [x] Configuration docs reflect the current compose setup.
- [x] No more conflicting references to diff-based vs change-aware sync in public docs.

## Notes
- Keep references to future diff-based improvements in the roadmap if we still plan incremental updates later.
