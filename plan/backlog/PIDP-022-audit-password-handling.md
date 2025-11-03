# Document Password Handling Constraints
- Type: Task
- Prerequisites: PIDP-019
- Status: Planned

## Context
The security audit flagged plaintext password storage. Pi-hole's API requires plaintext credentials, and `.NET` no longer recommends `SecureString`. We need to acknowledge the limitation and document safe handling guidance.

## Work Items
- [ ] Update `docs/security.md` (or create it) with a section explaining why Pi-hole credentials must remain plaintext in memory, referencing Microsoft guidance.
- [ ] Document recommended mitigations: secrets via env vars/orchestrator, avoid logging, keep authenticated sessions short, encourage credential rotation.
- [ ] Add a note in `AGENTS.md` / `docs/contributing.md` pointing to the security guidance.

## Acceptance Criteria
- [ ] Repository contains clear documentation addressing the audit finding and recommended mitigations.
- [ ] Contributors are pointed to the guidance when working with passwords.

## Notes
- Mention external secret managers (Vault, Key Vault, etc.) for production deployments.
