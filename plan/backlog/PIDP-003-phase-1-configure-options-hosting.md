# Configure Options & Hosting
- Type: Story
- Phase: 1
- Prerequisites: PIDP-002
- Status: Planned

## Context
We must wire environment-driven configuration into the .NET generic host, ensuring services read primary/secondary endpoints, credentials, interval, and dry-run flags consistently while enabling future reload support.

## Work Items
- [ ] Create options classes representing primary, secondary, and scheduler settings.
- [ ] Bind options via `IOptionsMonitor` using environment variables, default values, and structured sections.
- [ ] Implement FluentValidation validators for options (endpoints, hashed credentials, intervals) and hook them into startup.
- [ ] Register hosted services and dependency injection graph for the worker entry point.
- [ ] Ensure CLI entrypoint uses the same validated options pipeline.
- [ ] Add unit tests covering configuration binding and validation failures.

## Acceptance Criteria
- [ ] Application boots with sample environment variables and surfaces clear errors when required settings are missing or invalid.
- [ ] Options can be resolved via DI in unit tests with minimal setup, including validation coverage.
- [ ] Host wiring supports both background worker and CLI entrypoints with validated configuration.
- [ ] Logs redact sensitive values (hashed credentials) and surface validation errors succinctly.

## Notes
- Ensure sensitive values (hashed passwords) are marked as secrets in logs.
