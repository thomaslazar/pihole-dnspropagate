# Provision Pi-hole Sandbox
- Type: Task
- Phase: 2
- Prerequisites: PIDP-003
- Status: Completed

## Context
Teleporter client development needs a disposable Pi-hole environment to exercise `/api/auth` and `/api/teleporter` interactions without touching production systems. A lightweight sandbox (local Docker container or Testcontainers fixture) provides live API documentation and realistic responses for manual and automated validation.

## Work Items
- [x] Create a reusable Pi-hole sandbox definition (Docker Compose service or Testcontainers module) configured with deterministic credentials and HTTP ports.
- [x] Document environment variables and bootstrap steps so agents can launch the sandbox and access the Pi-hole admin/API UI locally.
- [x] Provide helper script or `dotnet test` fixture hook to start/stop the sandbox on demand for manual or automated runs.
- [x] Ensure sandbox teardown removes containers/networks to avoid residue between runs.
- [x] Record sample commands for retrieving `/api/docs` and verifying `/api/auth` login succeeds using configured credentials.

## Acceptance Criteria
- [x] Running the documented command(s) provisions a Pi-hole container reachable from the dev environment with the expected password.
- [x] Sandbox can be torn down cleanly, leaving no running Pi-hole containers or networks.
- [x] Developers have step-by-step instructions to inspect API docs and perform authenticated requests against the sandbox.
- [x] Teleporter client integration tests may reference this sandbox as a temporary target without additional manual setup.

## Notes
- Prefer using the official `pihole/pihole` image with pre-seeded environment variables; expose both web interface and DNS ports but DNS can be optional for API-only testing.
- Consider leveraging Testcontainers to keep future automation aligned with the manual sandbox definition.
- Some environments present the Docker socket as root-owned only; run the helper via `sudo` or re-login after adding the user to the `docker` group. Falling back to the container IP (from `docker inspect`) is acceptable when host port-forwarding is unavailable.
