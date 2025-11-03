# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

USER root

ENV DEBIAN_FRONTEND=noninteractive

# Base tooling
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        gnupg \
        jq \
        lsb-release \
        nodejs \
        npm \
        ripgrep && \
    rm -rf /var/lib/apt/lists/*

# Docker engine & compose from official repository
RUN install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc && \
    chmod a+r /etc/apt/keyrings/docker.asc && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo \"$VERSION_CODENAME\") stable" > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        docker-ce \
        docker-ce-cli \
        containerd.io \
        docker-buildx-plugin \
        docker-compose-plugin && \
    rm -rf /var/lib/apt/lists/*

# GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | gpg --dearmor -o /usr/share/keyrings/githubcli-archive-keyring.gpg && \
    chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" > /etc/apt/sources.list.d/github-cli.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends gh && \
    rm -rf /var/lib/apt/lists/*

# Allow vscode user to access docker socket
RUN usermod -aG docker vscode

# GitKraken CLI (for MCP integration)
RUN curl -fsSL -o /tmp/gk.deb https://github.com/gitkraken/gk-cli/releases/download/v3.1.40/gk_3.1.40_linux_amd64.deb && \
    apt-get update && \
    apt-get install -y /tmp/gk.deb && \
    rm -f /tmp/gk.deb && \
    rm -rf /var/lib/apt/lists/*

# Codex CLI
RUN npm install -g @openai/codex
