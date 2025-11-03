# Harden Example Environment Files
- Type: Task
- Prerequisites: PIDP-021
- Status: Planned

## Context
The security audit flagged weak placeholders in `.env.dev`. We should replace `changeme` values, reinforce guidance, and optionally validate against obvious weak passwords.

## Work Items
- [ ] Replace example passwords in `.env.dev` with neutral placeholders (e.g., `<PRIMARY_PIHOLE_PASSWORD>`).
- [ ] Update documentation to instruct developers to supply their own credentials via env vars or secret managers.

## Acceptance Criteria
- [ ] `.env.dev` no longer contains weak example passwords.
- [ ] Docs explain how to supply real credentials safely.

## Notes
- Double-check `.env*` remain gitignored.
