# Configuration & Deployment

This document captures the environment variables, container images, and compose examples required to run **pihole-dnspropagate**.

## Environment Variables

| Variable | Required | Description | Example |
| --- | --- | --- | --- |
| `PRIMARY_PIHOLE_URL` | Yes | Base URL for the authoritative Pi-hole instance that owns the DNS/CNAME records. | `http://pihole-primary.local` |
| `PRIMARY_PIHOLE_PASSWORD` | Yes | Pi-hole web password or application password for the primary instance. Plaintext is expected; Pi-hole handles hashing. | `changeme` |
| `SECONDARY_PIHOLE_URLS` | Yes | Comma-separated list of secondary Pi-hole base URLs that should receive the synchronized records. | `http://pihole-secondary-1.local,http://pihole-secondary-2.local` |
| `SECONDARY_PIHOLE_PASSWORDS` | Yes | Passwords for the secondary instances, comma separated, aligned with the `SECONDARY_PIHOLE_URLS` order. | `changeme,changeme` |
| `SECONDARY_PIHOLE_NAMES` | No | Optional friendly names for each secondary (used in logs and reporting). | `secondary-1,secondary-2` |
| `SYNC_INTERVAL` | No | Fallback interval between sync cycles when no cron is supplied. Defaults to 5 minutes. | `00:05:00` |
| `SYNC_CRON` | No | Cron expression controlling sync cadence. Takes precedence over `SYNC_INTERVAL` when set. | `*/10 * * * *` |
| `SYNC_DRY_RUN` | No | When set to `true`, the worker downloads archives but skips uploads. | `false` |
| `HTTP_TIMEOUT` | No | Per-request timeout for Pi-hole HTTP calls. | `00:00:30` |
| `LOG_LEVEL` | No | Minimum log level for the worker process. | `Information` |
| `HEALTH_PORT` | No | Port exposed by the in-process health endpoint. | `8080` |

> **Note**
> Pi-hole’s HTTP API expects plaintext credentials. If you run Pi-hole via Docker and want to change the admin password, use `docker exec <container> pihole -a -p` or follow the [Pi-hole docs](https://docs.pi-hole.net/core/pihole-command/#pihole-a) to generate the required hash for Pi-hole’s own `WEBPASSWORD`. The environment variables consumed by **pihole-dnspropagate** should remain plaintext so the API can authenticate correctly.

A ready-to-edit `.env.dev` template lives at the repository root. Duplicate or adapt it for production deployments and keep the secret values outside of source control.

## Docker Images

A multi-stage Dockerfile is provided at the repository root. Build it locally:

```bash
docker build -t pihole-dnspropagate:dev .
```

Run the worker directly:

```bash
docker run --env-file .env.dev --network pihole-sync --rm pihole-dnspropagate:dev
```

To trigger an immediate synchronization without waiting for the scheduler:

```bash
docker compose -f deploy/compose/docker-compose.dev.yaml run --rm pihole-dnspropagate sync-now
```

When `--dry-run` is omitted, the manual command inherits the `SYNC_DRY_RUN` value from the environment. Pass `--dry-run` (or `--dry-run:false`) explicitly to override the configured behaviour for a single invocation.

For publishing, tag the image appropriately (e.g., `ghcr.io/<org>/pihole-dnspropagate:<tag>`) and push to your registry of choice.

## Compose Examples

Two compose templates live under `deploy/compose/`:

- `docker-compose.dev.yaml` builds the image from source and is suited for local iteration.
- `docker-compose.prod.yaml` assumes an image already exists in a registry and expects the `pihole-sync` network to be created ahead of time (`docker network create pihole-sync`).

Start the development stack from the repository root:

```bash
docker compose -f deploy/compose/docker-compose.dev.yaml up -d
```

For production, point to your env file and image tag:

```bash
docker compose -f deploy/compose/docker-compose.prod.yaml --env-file .env.prod up -d
```

Both files map port `8080` from the container so you can reach the `/healthz` endpoint. Adjust the mapping or `HEALTH_PORT` value to fit your environment.

## Health Checks & Observability

The worker exposes JSON health data on `/healthz`. When running inside Docker, the compose examples publish it on `http://localhost:8080/healthz`. Integrate this endpoint with your orchestrator or monitoring stack (e.g., Docker health checks, Kubernetes `Probe`, Prometheus blackbox).

## Session Handling

Each synchronization run authenticates against Pi-hole via `/api/auth`, performs the download/upload workflow, and explicitly deletes the session (`DELETE /api/auth`) before exiting. This prevents buildup of stale sessions and avoids HTTP 429 responses from the API. Expect re-authentication on every run; ensure the credentials supplied in your environment variables remain valid.

## Next Steps

- Integrate image publishing into CI (e.g., GitHub Actions workflow pushing to GHCR).
- Add secrets management for production (e.g., Docker secrets or cloud-specific stores).
- Extend documentation with troubleshooting for common Pi-hole API failures as usage feedback is collected.
