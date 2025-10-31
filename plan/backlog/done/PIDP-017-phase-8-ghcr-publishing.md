# Publish Release Images to GHCR
- Type: Task
- Phase: 8
- Prerequisites: PIDP-016
- Status: Completed

## Context
To deliver runnable artifacts, the release workflow must build and push Docker images to `ghcr.io/thomaslazar/pihole-dnspropagate` with both semantic version and `latest` tags. Ensure credentials and metadata are handled securely.

## Work Items
- [x] Configure GHCR authentication (using `GITHUB_TOKEN` with packages write permissions) and repository settings.
- [x] Extend release workflow to build container and push versioned + latest tags.
- [x] Capture image digests and surface them in GitHub Release notes.
- [x] Provide manual workflow strategy for release-candidate or ad-hoc image builds without touching `latest`.
- [ ] Optionally generate SBOM or metadata for compliance.
- [x] Use Mermaid diagrams when illustrating the publishing pipeline.
- [x] Check whether GitHub MCP can manage required secrets/permissions; only request maintainer action if automation is unavailable.

## Acceptance Criteria
- [x] Release workflow pushes versioned and `latest` images to GHCR on successful runs.
- [x] Release notes include image tags/digests.
- [x] Documentation explains how to pull specific versions and how credentials are managed.

## Notes
- Reuse build outputs from CI to avoid double compilation where possible.
