# Pi-hole Sandbox

This sandbox provides disposable primary/secondary Pi-hole instances for manual Teleporter client exploration, archive processing, and API testing.

## Prerequisites

- Docker CLI available inside the devcontainer (install with `sudo apt-get update && sudo apt-get install -y docker.io` if `docker` is missing). The socket (`/var/run/docker.sock`) is already forwarded for you.
- Docker Compose v2 plugin (`docker compose`) or the classic `docker-compose` binary. The devcontainer installs the plugin automatically; if you run elsewhere, install `docker-compose-plugin`.
- Copy `deploy/pihole-sandbox/.env.example` to `.env` and adjust ports or passwords for both Pi-hole instances if needed.

```bash
cp deploy/pihole-sandbox/.env.example deploy/pihole-sandbox/.env
```

## Usage

Start the sandbox (run with `sudo` inside the devcontainer so Docker can talk to the forwarded socket). This launches both primary and secondary Pi-hole containers:

```bash
sudo \
./deploy/pihole-sandbox/sandbox.sh up
```

Check status or follow logs:

```bash
sudo \
./deploy/pihole-sandbox/sandbox.sh status
sudo \
./deploy/pihole-sandbox/sandbox.sh logs
```

Stop and remove all sandbox resources:

```bash
sudo \
./deploy/pihole-sandbox/sandbox.sh down
```

## Accessing the API and UI

- **Primary admin UI:** <http://localhost:8181/admin> (or the primary container IP reported by `sandbox.sh status`).
- **Secondary admin UI:** <http://localhost:8281/admin> (or the secondary container IP).
- **API documentation:** <http://localhost:8181/api/docs/> (primary endpoint).
- Authenticate (passwords defined in `.env`; adjust the host if using container IPs):

```bash
curl -s -X POST http://localhost:8081/api/auth \
  -H 'Content-Type: application/json' \
  -d '{"password":"admin123"}'
```

## Notes

- DNS is exposed on TCP/UDP port `1053` by default; adjust in `.env` if conflicts arise.
- Volumes (`pihole_data`, `dnsmasq_data`) persist between runs. Use `down` to remove them.
- Passwords are automatically set based on the `.env` file after `up`. If you need to reset manually:

  ```bash
  sudo docker exec pihole-primary pihole setpassword "<new-password>"
  sudo docker exec pihole-secondary pihole setpassword "<new-password>"
  ```

## Environment Variables for Tests

Integration tests expect the following variables when run against the sandbox:

- `SANDBOX_PIHOLE_PASSWORD` – matches the password configured in `.env`.
- `SANDBOX_PIHOLE_URL` – base URL for the primary Pi-hole (default `http://localhost:8181/`; **inside the devcontainer use the container IP instead of `localhost`**).
- `SANDBOX_PIHOLE_SECONDARY_URL` – base URL for the secondary Pi-hole (default `http://localhost:8281/`; likewise prefer the container IP inside the devcontainer).
- The sandbox is intended for development only; never reuse credentials in production.

## Devcontainer Tips & Troubleshooting

- **Always use `sudo` when calling `sandbox.sh`** (e.g., `sudo ./deploy/pihole-sandbox/sandbox.sh up`). The devcontainer mounts `/var/run/docker.sock` as root-owned, so plain `docker` commands will fail without escalation.
- **`localhost` endpoints do not resolve from inside the devcontainer.** Because Docker publishes the Pi-hole ports on the host network, `http://127.0.0.1:8181` fails from the container. Use the container IPs instead:

  ```bash
  PRIMARY_IP=$(sudo docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' pihole-primary)
  SECONDARY_IP=$(sudo docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' pihole-secondary)

  SANDBOX_PIHOLE_URL="http://$PRIMARY_IP/" \
  SANDBOX_PIHOLE_PASSWORD=primarypass \
  dotnet test --filter FullyQualifiedName=...
  ```

- **Wait for the Teleporter endpoint.** Right after seeding data or restarting containers, `/api/teleporter` can return transient errors while Pi-hole rebuilds archives. The integration tests include helper waits, but if you script manual calls, sleep a few seconds and retry.
- **Clean up when finished.** Run `sudo ./deploy/pihole-sandbox/sandbox.sh down` to remove the containers and volumes so future test runs start from a clean state.
