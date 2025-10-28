# Build Teleporter Client
- Type: Story
- Phase: 2
- Prerequisites: PIDP-003
- Status: Completed

## Context
The service needs a resilient HTTP layer to authenticate with Pi-hole via the `/api/auth` endpoint by POSTing JSON `{ "password": "..." }`, manage returned session metadata (SID, CSRF token, validity window, TOTP flag), and interact with the `/api/teleporter` download/upload endpoints using Flurl.Http and Polly. Subsequent requests should prefer the `X-FTL-SID` header for authentication (avoiding cookies unless required) and include the CSRF token when demanded by the API.

## Work Items
- [x] Implement authentication handshake against `/api/auth` submitting plaintext password/app-password, capturing session metadata (SID, CSRF, validity, TOTP flag) and refreshing on expiry.
- [x] Encapsulate Teleporter download/upload calls with retry policies using Flurl.Http + Polly, carrying SID via `X-FTL-SID` header per docs (avoid cookies).
- [x] Normalize base URLs (trailing slashes) to avoid malformed requests.
- [x] Design `ITeleporterClient` interface and Flurl-based implementation that surfaces session state per instance.
- [x] Create unit tests covering happy path, re-authentication, and transient failure scenarios.

## Acceptance Criteria
- [x] Client successfully downloads and uploads Teleporter archives in integration tests (or high-fidelity tests hitting mock endpoints).
- [x] Authentication retries on 401/403 and recovers without crashing the worker.
- [x] Errors from Teleporter interactions are logged with actionable detail for operators.
- [x] Unit tests verify retry/backoff and auth refresh flows.

## Notes
- Store session state per Pi-hole instance to avoid cross-contamination and refresh routes before SID expiry.
- `/api/teleporter` returns `application/zip`; ensure callers receive bytes and handle 401 responses by re-authenticating.
- Unit coverage with `TeleporterClientTests` exercises auth refresh and retry paths via Flurl's `HttpTest` harness.
