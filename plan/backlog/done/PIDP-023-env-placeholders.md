# Harden Example Environment Files
- Type: Task
- Prerequisites: PIDP-021
- Status: Completed

## Context
The security audit flagged weak placeholders in `.env.dev`. We should replace `changeme` values, reinforce guidance, and optionally validate against obvious weak passwords.

## Work Items
- [x] Replace example passwords in `.env.dev` with neutral placeholders (e.g., `<PRIMARY_PIHOLE_PASSWORD>`).
- [x] Update documentation to instruct developers to supply their own credentials via env vars or secret managers.

## Acceptance Criteria
- [x] `.env.dev` no longer contains weak example passwords.
- [x] Docs explain how to supply real credentials safely.

## Notes
- Double-check `.env*` remain gitignored.
