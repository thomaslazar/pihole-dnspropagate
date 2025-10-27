# Build Teleporter Client
- Type: Story
- Phase: 2
- Prerequisites: PIDP-003
- Status: Planned

## Context
The service needs a resilient HTTP layer to authenticate with Pi-hole, manage cookies/CSRF tokens, and interact with the Teleporter endpoints using Flurl.Http and Polly.

## Work Items
- [ ] Implement authentication handshake submitting hashed password, capturing cookies and CSRF tokens, and refreshing on expiry.
- [ ] Encapsulate Teleporter download/upload calls with retry policies using Flurl.Http + Polly.
- [ ] Normalize base URLs (trailing slashes) to avoid malformed requests.
- [ ] Design `ITeleporterClient` interface and Flurl-based implementation with cookie management.
- [ ] Create unit tests covering happy path, re-authentication, and transient failure scenarios.

## Acceptance Criteria
- [ ] Client successfully downloads and uploads Teleporter archives in integration tests.
- [ ] Authentication retries on 401/403 and recovers without crashing the worker.
- [ ] Errors from Teleporter interactions are logged with actionable detail for operators.
- [ ] Unit tests verify retry/backoff and auth refresh flows.

## Notes
- Store session state per Pi-hole instance to avoid cross-contamination.
