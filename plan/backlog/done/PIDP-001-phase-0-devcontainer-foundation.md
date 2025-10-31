# Establish Devcontainer Foundation
- Type: Task
- Phase: 0
- Prerequisites: None
- Status: Completed

## Context
We need a reproducible VS Code devcontainer that ships with .NET 9 tooling, Codex CLI, GitKraken CLI, Docker utilities, and the mandated extension set so contributors can work without host setup. This also ensures CODEX_HOME and config scaffolding exist from the start.

## Work Items
- [x] Author `.devcontainer/devcontainer.json` with required features, extension list, `containerEnv.CODEX_HOME`, and automatic tooling installs.
- [x] Build container image using official dotnet devcontainer base with Codex CLI and GitKraken CLI provisioned.
- [x] Create `.codex/config.toml` template per specification and ensure `.codex/` (except the template) is gitignored.
- [x] Validate container build locally: verify tooling availability, extensions, Codex configuration, and GitKraken CLI functionality.

## Acceptance Criteria
- [x] Opening the repository in VS Code launches the container with all specified tooling, extensions, and Codex configuration pre-installed.
- [x] Codex CLI reports the configured model and reasoning defaults using the committed template.
- [x] GitKraken CLI is invokable inside the container (`gk --version` succeeds).
- [x] `.codex/config.toml` remains tracked while other `.codex` artifacts are ignored by git.

## Notes
- Keep container image size reasonable by cleaning apt caches after installs.
