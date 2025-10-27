# 🧩 pihole-dnspropagate
**Synchronize Pi-hole local DNS and CNAME records across multiple Pi-hole instances — nothing more, nothing less.**

---

## 🧠 Overview
**pihole-dnspropagate** is a lightweight, containerized service designed to keep your **local DNS** and **CNAME records** consistent across multiple Pi-hole installations.  

Existing sync tools (like *Nebula Sync*) replicate entire Pi-hole configurations, but they often overlook the local DNS and CNAME data stored separately in the Pi-hole API. **pihole-dnspropagate** focuses exclusively on this missing piece, using the Pi-hole API to pull records from a primary instance and apply them to others.

---

## ⚙️ Features
- 🔄 **Automatic propagation** of local DNS and CNAME records between Pi-holes.  
- 🧭 **Source-defined sync** – choose one “primary” Pi-hole as the authoritative source.  
- 🕒 **Periodic updates** – customizable sync interval.  
- 🧰 **API-based updates** – no file-level or full-config syncing required.  
- 🐳 **Docker-ready** – runs as a self-contained service.  

---

## 🚀 How It Works
1. The container periodically queries the *primary* Pi-hole instance using its API key.  
2. It fetches all local DNS and CNAME records.  
3. It compares them to each target Pi-hole instance.  
4. Any missing or outdated records are added or updated remotely through the Pi-hole API.  

This ensures your network stays consistent even if you manage several Pi-holes in different locations or VLANs.

---

## 🧩 Example Configuration
Example environment variables for Docker Compose:

```yaml
services:
  pihole-dnspropagate:
    image: ghcr.io/yourusername/pihole-dnspropagate:latest
    container_name: pihole-dnspropagate
    restart: unless-stopped
    environment:
      - PRIMARY_PIHOLE_URL=http://pihole-master.local
      - PRIMARY_PIHOLE_API_KEY=your_master_api_key
      - TARGETS=http://pihole-node1.local,http://pihole-node2.local
      - TARGET_API_KEYS=key1,key2
      - SYNC_INTERVAL=300   # seconds
```

---

## 🧰 Requirements
- Pi-hole **v6 or later** (API-enabled).  
- API access enabled on all target Pi-hole instances.  
- Docker or Podman environment.  

---

## 🧱 Roadmap
- ✅ Basic DNS + CNAME propagation  
- 🔄 Diff-based sync to minimize API calls  
- 📊 Metrics endpoint for Prometheus  
- 🕹️ CLI support for manual sync trigger  

---

## 🧑‍💻 Contributing
Contributions, issues, and feature requests are welcome!  
Feel free to open a PR or an issue to discuss new ideas.

---

## 📜 License
MIT License © 2025 Your Name  
