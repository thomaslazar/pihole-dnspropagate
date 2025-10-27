# Scaffold .NET Solution Structure
- Type: Task
- Phase: 0
- Prerequisites: PIDP-001
- Status: Planned

## Context
After the devcontainer is ready, the repository needs baseline .NET solution files, project structure, and shared build configuration to anchor future development.

## Work Items
- [ ] Create `global.json` pinning the .NET 9 SDK.
- [ ] Add `Directory.Build.props` with shared analyzer and nullable settings.
- [ ] Initialize solution file (`pihole-dnspropagate.sln`) with projects for `src/PiholeDnsPropagate`, `src/PiholeDnsPropagate.Worker`, and `tests/PiholeDnsPropagate.Tests`.
- [ ] Add initial `PackageReference` entries for planned libraries (Flurl.Http, Polly, SharpCompress, Tomlyn, Cronos, Spectre.Console.Cli, Serilog + console sink, FluentValidation, DotNet.Testcontainers in tests).
- [ ] Verify packages restore and build successfully within the devcontainer environment.
- [ ] Stub projects with minimal `Program.cs` / test placeholders to ensure successful `dotnet build`.

## Acceptance Criteria
- [ ] `dotnet restore` and `dotnet build` from the repository root succeed using the devcontainer toolchain without additional feed configuration.
- [ ] Solution contains the defined projects, shared build configuration, and package references aligned with the implementation plan.
- [ ] Project directories exist with placeholder code and tests ready for later phases without unused reference warnings.

## Notes
- Keep initial project code minimal; functional logic arrives in later phases.
