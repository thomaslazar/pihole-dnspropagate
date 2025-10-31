# Process Teleporter Archives
- Type: Story
- Phase: 3
- Prerequisites: PIDP-004
- Status: Completed

## Context
To sync records we must read Pi-hole Teleporter zip archives, extract `pihole/pihole.toml`, and update the TOML `[dns].hosts` and `cnameRecords` arrays with values from the primary instance while preserving every other file untouched.

## Work Items
- [x] Implement SharpCompress-based reader for Teleporter archives (zip).
- [x] Parse `pihole/pihole.toml` using Tomlyn, capturing `[dns].hosts` and `cnameRecords` arrays.
- [x] Replace secondary TOML arrays with primary data while preserving other sections and formatting; leave all other files untouched.
- [x] Normalize encoding and line endings across generated files.
- [x] Develop helper utilities and fixtures for generating deterministic archives in tests featuring representative TOML content.
- [x] Verify output zip preserves original directory structure and file metadata (except `pihole.toml` contents).
- [x] Document fixture usage for contributors.

## Acceptance Criteria
- [x] Generated archives load successfully when imported back into Pi-hole with updated Local DNS and CNAME entries.
- [x] Line endings and encoding remain consistent across platforms, preserving TOML structure.
- [x] Tests can create archives with varied record sets quickly, without requiring live Pi-hole instances, and the zip structure matches the original aside from TOML changes.

## Notes
- Avoid loading entire archives into memory when unnecessary; stream where possible.
- Archive processor targets `etc/pihole/pihole.toml` and preserves metadata via SharpCompress; fixtures under `tests/.../Teleporter/Fixtures` demonstrate deterministic archive generation.
- Sandbox integration (`TeleporterSandboxTests.ProcessArchiveAndApplyToSecondary`) validates end-to-end replacement against the secondary Pi-hole container.
