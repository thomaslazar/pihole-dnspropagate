# Publish Release Images to GHCR
- Type: Task
- Phase: 8
- Prerequisites: PIDP-016
- Status: Planned

## Context
To deliver runnable artifacts, the release workflow must build and push Docker images to `ghcr.io/thomaslazar/pihole-dnspropagate` with both semantic version and `latest` tags. Ensure credentials and metadata are handled securely.

## Work Items
- [ ] Configure GHCR authentication (PAT or `GITHUB_TOKEN`) and repository permissions.
- [ ] Extend release workflow to build container, run smoke tests, and push versioned + latest tags.
- [ ] Capture image digests and surface them in GitHub Release notes.
- [ ] Optionally generate SBOM or metadata for compliance.
- [ ] Use Mermaid diagrams when illustrating the publishing pipeline.
- [ ] Check whether GitHub MCP can manage required secrets/permissions; only request maintainer action if automation is unavailable.

## Acceptance Criteria
- [ ] Release workflow pushes versioned and `latest` images to GHCR on successful runs.
- [ ] Release notes include image tags/digests.
- [ ] Documentation explains how to pull specific versions and how credentials are managed.

## Notes
- Reuse build outputs from CI to avoid double compilation where possible.
