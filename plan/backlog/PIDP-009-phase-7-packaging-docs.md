# Finalize Packaging & Docs
- Type: Story
- Phase: 7
- Prerequisites: PIDP-008
- Status: Planned

## Context
We need to package the service as a Docker image, provide docker-compose examples, and document configuration for contributors and operators.

## Work Items
- [ ] Author multi-stage Dockerfile optimized for runtime footprint.
- [ ] Create `docker-compose.yaml` examples (dev/prod) referencing documented env vars.
- [ ] Update README and `docs/configuration.md` with deployment guidance.
- [ ] Provide `.env.dev` template with placeholders and guidance on hashing Pi-hole credentials.

## Acceptance Criteria
- [ ] Docker image builds locally and via CI with expected tags.
- [ ] Compose example launches service and connects to Pi-hole instances.
- [ ] Documentation reflects final command usage and config options, including credential management guidance.

## Notes
- Consider publishing container to GHCR as part of release process (future work).
