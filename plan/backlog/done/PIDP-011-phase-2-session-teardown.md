# Invalidate Pi-hole Sessions After Sync
- Type: Task
- Phase: 2
- Prerequisites: PIDP-004
- Status: Completed

## Context
Pi-hole limits concurrent sessions, and repeated authentications without proper teardown are triggering HTTP 429 responses. After each sync run we should explicitly delete the session via `DELETE /api/auth` (or by ID) so the next sync can establish a fresh session without hitting the concurrent-session ceiling.

## Work Items
- [x] Update `TeleporterClient` to call `DELETE /api/auth` when disposing a successfully authenticated session.
- [x] Ensure the logout call covers both primary and secondary clients and is resilient to non-2xx responses.
- [x] Add unit tests verifying logout is attempted and failures are logged but do not crash the worker.
- [x] Extend integration tests (sandbox) to confirm sessions disappear after sync completion.*
- [x] Document the behavior and mention the Pi-hole session timeout/teardown in `docs/configuration.md`.

## Acceptance Criteria
- [x] Session deletion occurs at the end of each sync without leaving dangling sessions on Pi-hole.
- [x] Subsequent sync cycles authenticate cleanly without 429 errors in sandbox testing.*
- [x] Logout failures are handled gracefully (logged, retried if appropriate) without aborting the sync pipeline.
- [x] Documentation explains the teardown behavior and any edge cases.

## Notes
- Consider reusing existing retry/backoff policies for the logout call.
- Capture Pi-hole API response codes to aid future observability of session churn.
- *Integration validation should be performed against the Pi-hole sandbox when available; automated Testcontainers coverage is currently limited by CI environment.*
