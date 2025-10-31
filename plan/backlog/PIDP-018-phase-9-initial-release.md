# Cut Initial Release v1.0.0
- Type: Task
- Phase: 9
- Prerequisites: PIDP-015, PIDP-016, PIDP-017
- Status: Planned

## Context
Once versioning, automation, and publishing are in place, execute the first tagged release (`v1.0.0`). This validates the end-to-end process and seeds the GitHub Releases history.

## Work Items
- [ ] Confirm backlog scope for v1.0.0 and ensure `develop` is up to date with main.
- [ ] Run the release workflow (or manual fallback) to tag `v1.0.0`.
- [ ] Review GitHub Release notes, attach artifacts if needed, and publish.
- [ ] Communicate release summary (README, docs, changelog update).
- [ ] When documenting the inaugural release, prefer Mermaid diagrams for process overviews.

## Acceptance Criteria
- [ ] Tag `v1.0.0` exists in the repository and maps to `main`.
- [ ] GHCR hosts versioned and latest images for the release.
- [ ] GitHub Release is published with accurate notes and references `v1.0.0`.

## Notes
- Capture follow-up tasks for any issues discovered during the inaugural release run.
