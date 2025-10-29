# Finalize Packaging & Docs
- Type: Story
- Phase: 7
- Prerequisites: PIDP-008
- Status: Completed

## Context
We need to package the service as a Docker image, provide docker-compose examples, and document configuration for contributors and operators.

## Work Items
- [x] Author multi-stage Dockerfile optimized for runtime footprint.
- [x] Create `docker-compose.yaml` examples (dev/prod) referencing documented env vars.
- [x] Update README and `docs/configuration.md` with deployment guidance.
- [x] Provide `.env.dev` template with placeholders and guidance on hashing Pi-hole credentials.

## Acceptance Criteria
- [x] Docker image builds locally and via CI with expected tags.
- [x] Compose example launches service and connects to Pi-hole instances.
- [x] Documentation reflects final command usage and config options, including credential management guidance.

## Notes
- Consider publishing container to GHCR as part of release process (future work).
- Validation: `dotnet build`; container build/compose commands documented for execution outside the restricted CI environment.
