# Configure Options & Hosting
- Type: Story
- Phase: 1
- Prerequisites: PIDP-002
- Status: Completed

## Context
We must wire environment-driven configuration into the .NET generic host, ensuring services read primary/secondary endpoints, credentials, interval, and dry-run flags consistently while enabling future reload support. Configuration should bind the following environment variables (with `appsettings` parity for future use):

- `PRIMARY_PIHOLE_URL`, `PRIMARY_PIHOLE_PASSWORD`
- `SECONDARY_PIHOLE_URLS`, `SECONDARY_PIHOLE_PASSWORDS` (comma-separated lists)
- `SYNC_INTERVAL` or `SYNC_CRON`
- `SYNC_DRY_RUN`, `LOG_LEVEL`, `HEALTH_PORT`, `HTTP_TIMEOUT`

## Work Items
- [x] Create options classes representing primary, secondary, and scheduler settings (plaintext Pi-hole passwords hashed internally).
- [x] Bind options via `IOptionsMonitor` using environment variables, default values, and structured sections.
- [x] Implement FluentValidation validators for options (endpoints, plaintext passwords, intervals) and hook them into startup; hash credentials after validation within the configuration pipeline.
- [x] Register hosted services and dependency injection graph for the worker entry point.
- [x] Ensure CLI entrypoint uses the same validated options pipeline.
- [x] Add unit tests covering configuration binding and validation failures.

## Acceptance Criteria
- [x] Application boots with sample environment variables and surfaces clear errors when required settings are missing or invalid.
- [x] Options can be resolved via DI in unit tests with minimal setup, including validation coverage.
- [x] Host wiring supports both background worker and CLI entrypoints with validated configuration.
- [x] Logs redact sensitive values (plaintext and hashed passwords) and surface validation errors succinctly.

## Notes
- Ensure sensitive values (plaintext passwords) are marked as secrets in logs and hash them only once during startup.
