# Process Teleporter Archives
- Type: Story
- Phase: 3
- Prerequisites: PIDP-004
- Status: Planned

## Context
To sync records we must read Pi-hole Teleporter zip archives, extract `pihole/pihole.toml`, and update the TOML `[dns].hosts` and `cnameRecords` arrays with values from the primary instance while preserving every other file untouched.

## Work Items
- [ ] Implement SharpCompress-based reader for Teleporter archives (zip).
- [ ] Parse `pihole/pihole.toml` using Tomlyn, capturing `[dns].hosts` and `cnameRecords` arrays.
- [ ] Replace secondary TOML arrays with primary data while preserving other sections and formatting; leave all other files untouched.
- [ ] Normalize encoding and line endings across generated files.
- [ ] Develop helper utilities and fixtures for generating deterministic archives in tests featuring representative TOML content.
- [ ] Verify output zip preserves original directory structure and file metadata (except `pihole.toml` contents).
- [ ] Document fixture usage for contributors.

## Acceptance Criteria
- [ ] Generated archives load successfully when imported back into Pi-hole with updated Local DNS and CNAME entries.
- [ ] Line endings and encoding remain consistent across platforms, preserving TOML structure.
- [ ] Tests can create archives with varied record sets quickly, without requiring live Pi-hole instances, and the zip structure matches the original aside from TOML changes.

## Notes
- Avoid loading entire archives into memory when unnecessary; stream where possible.
