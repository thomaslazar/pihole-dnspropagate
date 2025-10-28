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

Start the sandbox (use `sudo` if your Docker socket is root-owned). This launches both primary and secondary Pi-hole containers:

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

- **Primary admin UI:** <http://localhost:8081/admin> (or the primary container IP reported by `sandbox.sh status`).
- **Secondary admin UI:** <http://localhost:8082/admin> (or the secondary container IP).
- **API documentation:** <http://localhost:8081/api/docs/> (primary endpoint).
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
- `SANDBOX_PIHOLE_URL` – base URL for the primary Pi-hole (default `http://localhost:8081/` or container IP).
- `SANDBOX_PIHOLE_SECONDARY_URL` – base URL for the secondary Pi-hole (default `http://localhost:8082/` or container IP).
- The sandbox is intended for development only; never reuse credentials in production.
