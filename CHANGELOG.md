# Changelog

All notable changes to this project are documented in this file. The format roughly follows [Keep a Changelog](https://keepachangelog.com/) and adheres to [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2025-11-03
### Added
- Documented the GitHub Project-based backlog workflow in `AGENTS.md` and `docs/contributing.md`.
- Installed the GitHub CLI in the devcontainer to streamline project automation workflows.

### Changed
- Clarified README and configuration guidance around change-aware synchronization and credential handling.
- Replaced sample environment passwords with placeholders and refreshed agent operations guidance.

### Fixed
- Updated Microsoft.Extensions.* options and Serilog logging packages to their latest compatible versions.
- Bumped Microsoft.NET.Test.Sdk and Coverlet packages to keep CI and coverage tooling current.
- Adopted the latest GitHub Actions runners and Docker build action releases via Dependabot.

### Testing
- `dotnet build`
- `dotnet test --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutput=TestResults/coverage/ /p:CoverletOutputFormat=cobertura%2copencover /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total`

## [1.0.0] - 2025-10-31
### Added
- Initial release of the Pi-hole DNS propagate worker with Teleporter client, archive processing pipeline, sync coordinator, scheduler, manual CLI trigger, and health endpoint.
- Testcontainers-based integration scaffolding and Pi-hole sandbox automation.
- Packaging assets (Dockerfile, compose samples) and release automation for GHCR + GitHub Releases.

