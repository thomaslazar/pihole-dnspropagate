# 🧩 pihole-dnspropagate
[![CI](https://github.com/thomaslazar/pihole-dnspropagate/actions/workflows/ci.yml/badge.svg)](https://github.com/thomaslazar/pihole-dnspropagate/actions/workflows/ci.yml)

**Synchronize Pi-hole local DNS and CNAME records across multiple Pi-hole instances — nothing more, nothing less.**

---

## 🧠 Overview
**pihole-dnspropagate** is a lightweight, containerized service designed to keep your **local DNS** and **CNAME records** consistent across multiple Pi-hole installations.  

Existing sync tools (like *Nebula Sync*) replicate entire Pi-hole configurations, but they often overlook the local DNS and CNAME data stored separately in the Pi-hole API. **pihole-dnspropagate** focuses exclusively on this missing piece, using the Pi-hole API to pull records from a primary instance and apply them to others.

---

## ⚙️ Features
- 🔄 **Automatic propagation** of local DNS and CNAME records between Pi-holes.  
- 🧭 **Source-defined sync** – choose one “primary” Pi-hole as the authoritative source.  
- 🕒 **Periodic updates** – scheduler supports fixed intervals or cron expressions.  
- 🔧 **Manual sync CLI** – trigger `sync-now` from the container without waiting for the scheduler.  
- 🧰 **API-based updates** – no file-level or full-config syncing required.  
- 📈 **Health endpoint** – JSON status exposed on `/healthz` for readiness checks.  
- 🐳 **Docker-ready** – multi-stage image and compose templates included.  

---

## 🚀 How It Works
1. The container periodically queries the *primary* Pi-hole instance using its API key.  
2. It fetches all local DNS and CNAME records.  
3. It compares them to each target Pi-hole instance.  
4. Any missing or outdated records are added or updated remotely through the Pi-hole API.  

This ensures your network stays consistent even if you manage several Pi-holes in different locations or VLANs.

---

## 🧩 Example Configuration
Example docker-compose snippet using the environment variables consumed by the worker:

```yaml
services:
  pihole-dnspropagate:
    image: ghcr.io/thomaslazar/pihole-dnspropagate:latest
    restart: unless-stopped
    env_file:
      - ./pihole-dnspropagate.env
    networks:
      - pihole-sync

networks:
  pihole-sync:
    driver: bridge
```

And the accompanying `pihole-dnspropagate.env` could look like:

```bash
PRIMARY_PIHOLE_URL=http://pihole-primary.local
PRIMARY_PIHOLE_PASSWORD=super-secret
SECONDARY_PIHOLE_URLS=http://pihole-secondary-1.local,http://pihole-secondary-2.local
SECONDARY_PIHOLE_PASSWORDS=super-secret,super-secret
SECONDARY_PIHOLE_NAMES=secondary-1,secondary-2
SYNC_INTERVAL=00:05:00
# SYNC_CRON=*/10 * * * *
SYNC_DRY_RUN=false
LOG_LEVEL=Information
HEALTH_PORT=8080
```

The full list of configuration options and compose templates lives in [`docs/configuration.md`](docs/configuration.md).

---

## 🧰 Requirements
- Pi-hole **v6 or later** with HTTP API enabled.  
- Admin or application passwords for the primary and secondary instances.  
- Docker / Podman for container deployments, or .NET 9 SDK for local builds.  
- Network connectivity from the worker to each Pi-hole host (DNS/IP reachability).  

---

## 🧱 Roadmap
- ✅ Basic DNS + CNAME propagation  
- ✅ Scheduler + manual CLI + health endpoint  
- 🔄 Session teardown & smarter backoff to avoid rate limits  
- 📊 Metrics endpoint for Prometheus  
- 🗂️ Diff-based sync to minimize uploads  
- 🚀 CI image publishing to GHCR  

---

## 🧪 Testing & Coverage
- Run the full test suite with coverage enforcement:
  ```bash
  dotnet test /p:CollectCoverage=true /p:CoverletOutput=TestResults/coverage/ /p:CoverletOutputFormat=cobertura%2copencover /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
  ```
- Integration tests automatically provision a primary and secondary Pi-hole via Testcontainers; ensure Docker is available before running the suite.

## 🐳 Container Usage
- Build the worker image locally with:
  ```bash
  docker build -t pihole-dnspropagate:dev .
  ```
- Launch the service using the development compose stack:
  ```bash
  docker compose -f deploy/compose/docker-compose.dev.yaml up -d
  ```
- Configure environment variables via `.env.dev` (or your own copy) and review advanced deployment guidance in `docs/configuration.md`.
- Trigger an immediate synchronization without waiting for the scheduler:
  ```bash
  docker compose run --rm pihole-dnspropagate sync-now
  ```

---

## 🧑‍💻 Contributing
Contributions, issues, and feature requests are welcome!  
Feel free to open a PR or an issue to discuss new ideas.

> **Development Note**  
> This project is primarily developed with OpenAI Codex / agentic tooling, and every change is reviewed and approved by a human maintainer before landing in the main branch.

---

## 📜 License
MIT License © 2025 Thomas Lazar
