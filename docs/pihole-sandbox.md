# Pi-hole Sandbox

This sandbox provides a disposable Pi-hole instance for manual Teleporter client exploration and API testing.

## Prerequisites

- Docker CLI available inside the devcontainer (install with `sudo apt-get update && sudo apt-get install -y docker.io` if `docker` is missing). The socket (`/var/run/docker.sock`) is already forwarded for you.
- Docker Compose v2 plugin (`docker compose`) or the classic `docker-compose` binary. The devcontainer installs the plugin automatically; if you run elsewhere, install `docker-compose-plugin`.
- Copy `deploy/pihole-sandbox/.env.example` to `.env` and adjust ports or passwords if needed.

```bash
cp deploy/pihole-sandbox/.env.example deploy/pihole-sandbox/.env
```

## Usage

Start the sandbox (use `sudo` if your Docker socket is root-owned):

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

- Pi-hole Admin UI: <http://localhost:8081/admin> (use the container IP reported by `sandbox.sh status` if the host port isn’t reachable).
- API documentation: <http://localhost:8081/api/docs/>
- Authenticate (password from `.env`; adjust the host if using the container IP):

```bash
curl -s -X POST http://localhost:8081/api/auth \
  -H 'Content-Type: application/json' \
  -d '{"password":"admin123"}'
```

## Notes

- DNS is exposed on TCP/UDP port `1053` by default; adjust in `.env` if conflicts arise.
- Volumes (`pihole_data`, `dnsmasq_data`) persist between runs. Use `down` to remove them.
- If authentication fails on first run, reset the password inside the container: `sudo docker exec pihole-sandbox pihole setpassword "$(grep WEBPASSWORD deploy/pihole-sandbox/.env | cut -d= -f2)"`.
- The sandbox is intended for development only; never reuse credentials in production.
