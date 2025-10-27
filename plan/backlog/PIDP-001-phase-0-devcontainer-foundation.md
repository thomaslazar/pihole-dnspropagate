# Establish Devcontainer Foundation
- Type: Task
- Phase: 0
- Prerequisites: None
- Status: Planned

## Context
We need a reproducible VS Code devcontainer that ships with .NET 9 tooling, Codex CLI, GitKraken CLI, Docker utilities, and the mandated extension set so contributors can work without host setup. This also ensures CODEX_HOME and config scaffolding exist from the start.

## Work Items
- [ ] Author `.devcontainer/devcontainer.json` with required features, extension list, `containerEnv.CODEX_HOME`, and automatic .NET tooling installs.
- [ ] Build `Dockerfile` installing .NET 9 SDK, Codex CLI (latest), GitKraken CLI, Docker CLI, curl, unzip, and other utilities; clean caches to minimize size.
- [ ] Create `.codex/config.toml` template per specification and ensure `.codex/` (except the template) is gitignored.
- [ ] Validate container build locally: verify tooling availability, extensions, Codex configuration, and GitKraken CLI functionality.

## Acceptance Criteria
- [ ] Opening the repository in VS Code launches the container with all specified tooling, extensions, and Codex configuration pre-installed.
- [ ] Codex CLI reports the configured model and reasoning defaults using the committed template.
- [ ] GitKraken CLI is invokable inside the container (`gk --version` succeeds).
- [ ] `.codex/config.toml` remains tracked while other `.codex` artifacts are ignored by git.

## Notes
- Keep container image size reasonable by cleaning apt caches after installs.
