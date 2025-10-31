# Align Commit & Deploy Guidance
- Type: Task
- Phase: 8
- Prerequisites: PIDP-017, PIDP-019
- Status: Completed

## Context
Agents need clearer instructions for elevated commands, commit author identity, and the prod compose network. We also need a shared configuration that sets the canonical committer email (`codexAI@razal.de`).

## Work Items
- [x] Add `.codex/committer.env` capturing `GIT_AUTHOR_NAME` and `GIT_AUTHOR_EMAIL` (defaulting to codexAI@razal.de).
- [x] Document in `AGENTS.md` how to source `sudo docker …` commands, configure Git identity from `.codex/committer.env`, avoid inline `git commit -m` messages, and verify commits post-recording.
- [x] Update `docs/contributing.md` (if needed) to point contributors to the committer configuration.
- [x] Modify `deploy/compose/docker-compose.prod.yaml` so it defines the local `pihole-sync` network (matching the dev compose file) instead of expecting an external network.

## Acceptance Criteria
- [x] Repository includes `.codex/committer.env` and guidance referencing it.
- [x] `AGENTS.md` (and contributing docs if applicable) describe sudo use, commit message workflow, and identity setup.
- [x] Prod compose file creates the `pihole-sync` network just like the dev compose file.

## Notes
- Future contributors can override `.codex/committer.env` locally if a different identity is required.
