# 🧩 pihole-dnspropagate
[![PR Validation](https://github.com/thomaslazar/pihole-dnspropagate/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/thomaslazar/pihole-dnspropagate/actions/workflows/pr-validation.yml)

**Synchronize Pi-hole local DNS and CNAME records across multiple Pi-hole instances — nothing more, nothing less.**

> Latest release: [v1.0.0](https://github.com/thomaslazar/pihole-dnspropagate/releases/tag/v1.0.0) (2025-10-31)

---

## 🧠 Overview
**pihole-dnspropagate** is a lightweight, containerized service designed to keep your **local DNS** and **CNAME records** consistent across multiple Pi-hole installations.  

Existing sync tools (like *Nebula Sync*) replicate entire Pi-hole configurations, but they usually don't support syncing of local DNS and CNAME data stored only in the Pi-hole configuration. **pihole-dnspropagate** focuses exclusively on this missing piece, using the Pi-hole API to pull records from a primary instance and apply them to others.

---

## ⚙️ Features
- 🔄 **Automatic propagation** of local DNS and CNAME records between Pi-holes.  
- 🧭 **Source-defined sync** – choose one “primary” Pi-hole as the authoritative source.  
- 🕒 **Periodic updates** – scheduler supports fixed intervals or cron expressions.  
- ♻️ **Diff-aware sync** – secondaries are updated only when DNS/CNAME changes are detected.  
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
- ✅ Session teardown after each sync (PIDP-011)  
- 🔄 Smarter backoff for Pi-hole rate limits  
- 🗂️ Diff-based sync to minimize uploads  
- 🚀 CI image publishing to GHCR  

---

## 🧑‍💻 Contributing
Contributions, issues, and feature requests are welcome!  
Feel free to open a PR or an issue to discuss new ideas.

Consult the [contributing guide](docs/contributing.md) for environment setup, testing, container workflows, and our release checklist.

> **Development Note**  
> This project is primarily developed with OpenAI Codex / agentic tooling, and every change is reviewed and approved by a human maintainer before landing in the main branch.

---

## 📜 License
MIT License © 2025 Thomas Lazar
