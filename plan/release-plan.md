# Release Enablement Plan

This document describes the governance, automation, and documentation work needed to move the repository from the current `develop` state to a repeatable release process that ships signed container images and tagged versions.

## Target Outcomes
- Adopt a branching model that separates production-ready code from ongoing development (`main`, `develop`, short-lived feature/hotfix branches).
- Protect `main` so it only changes through reviewed pull requests backed by passing CI.
- Publish signed Docker images to `ghcr.io/thomaslazar/pihole-dnspropagate` for every release (and optionally for pre-release builds).
- Automate GitHub Releases, including version tagging and changelog notes.
- Provide developer-facing documentation that explains how to cut releases and how versioning is managed (starting with `v1.0.0`).

## Branch Strategy
- `main`: production branch. Contains only what is shipped. Protected: require PRs, passing CI, linear history, and maintainer approval.
- `develop`: integration branch. Feature and bugfix branches branch from here and merge back via PR. Recommended protection: require CI.
- `feature/<name>` or `bugfix/<name>`: short-lived branches targeting `develop`.
- `hotfix/<name>`: created from `main` when an urgent production fix is required; merges into `main` (release) and `develop`.
- Document naming conventions and merge expectations in `AGENTS.md` and CONTRIBUTING guidelines.

## Versioning & Release Procedure
- Adopt semantic versioning with a `VERSION` file (or MSBuild `VersionPrefix`) managed in the repo. Initial release when the plan lands: `1.0.0`.
- Standard release steps (after plan implementation):
  1. Ensure `develop` is green; open PR `develop` → `main`.
  2. CI on the PR runs build, tests, and container build (without publish).
  3. Once merged, a release workflow bumps the version (if needed), creates a tag `vX.Y.Z`, publishes container images, and drafts a GitHub Release with notes.
  4. Any follow-up hotfix bumps a patch version; new features go through `develop`.
- Maintain `docs/release-process.md` with role-specific instructions: pre-release checklist, commit format, tagging rules, rollback steps.

## Automation Workstreams
- **CI Validation (existing pipeline extension)**
  - dotnet build/test on PRs targeting `develop` and `main`.
  - Optional coverage upload and status checks for branch protection.
- **Container Build & Publish**
  - GitHub Action triggered on merges to `main` (and on tags) that:
    - Builds the docker image.
    - Runs integration tests (or smoke tests) with sandbox if available.
    - Logs in to GHCR using repository secrets.
    - Pushes `${version}` and `latest` tags.
- **Release Workflow**
  - Separate workflow triggered by tags `v*` or by merge to `main`.
  - Generates release notes (using commits or curated fragments).
  - Publishes GitHub Release with container digests.
  - Optionally uploads SBOM or other compliance artifacts.
- **Branch Protection & Settings**
  - Configure protections through repository settings or automation (e.g., GitHub CLI).
  - Enforce CODEOWNERS for `main` if desired.

## Documentation & Guidance
- Update `AGENTS.md` and `docs/contributing.md` with:
  - Branching model.
  - Release versioning policy.
  - Summary of automated workflows and manual steps.
- Add `docs/release-process.md` (see requirements above) after automation lands.
- Provide quick-start commands for cutting a release locally (for emergencies) while clarifying that normal releases are workflow-driven.

## Backlog Items to Create
1. Define branch protection rules and document the branching model.
2. Implement GH Actions for PR validation (build/test) and container publishing.
3. Implement release workflow that tags, publishes images, and creates GitHub Releases.
4. Introduce version management (VERSION file/MSBuild property) and release documentation updates.
5. Populate `docs/release-process.md` describing how to ship `v1.0.0` and subsequent releases.

## Initial Release (`v1.0.0`)
- Once the plan, automation, and documentation are in place, run the release workflow to produce `v1.0.0`.
- Ensure the release notes summarize the milestone features and reference the initial backlog completion.
- After tagging `v1.0.0`, update roadmap/docs to point future work at `develop`.

This plan should be refined into backlog items (PIDP sequence) before execution so progress is tracked alongside the rest of the project.
