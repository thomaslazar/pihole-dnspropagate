#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.yaml"
ENV_FILE="${SCRIPT_DIR}/.env"

usage() {
  cat <<USAGE
Usage: $(basename "$0") <up|down|status|logs>

Commands:
  up      Launch the Pi-hole sandbox in detached mode.
  down    Stop the sandbox and remove containers and volumes.
  status  Show current container state.
  logs    Tail the Pi-hole container logs (Ctrl+C to exit).

Create ${ENV_FILE} (copy from .env.example) before running.
USAGE
}

ensure_env() {
  if [[ ! -f "${ENV_FILE}" ]]; then
    echo "Environment file '${ENV_FILE}' not found. Copy .env.example and adjust settings." >&2
    exit 1
  fi
}

ensure_compose() {
  if command -v docker >/dev/null 2>&1; then
    if docker compose version >/dev/null 2>&1; then
      COMPOSE_CMD=(docker compose)
      return
    fi
  fi

  if command -v docker-compose >/dev/null 2>&1; then
    COMPOSE_CMD=(docker-compose)
    return
  fi

  echo "Docker Compose is required. Install the docker-compose-plugin or docker-compose CLI." >&2
  exit 1
}

cmd=${1:-}
if [[ -z "${cmd}" ]]; then
  usage
  exit 1
fi

ensure_env
ensure_compose

case "${cmd}" in
  up)
    "${COMPOSE_CMD[@]}" --file "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d
    ;;
  down)
    "${COMPOSE_CMD[@]}" --file "${COMPOSE_FILE}" --env-file "${ENV_FILE}" down -v
    ;;
  status)
    "${COMPOSE_CMD[@]}" --file "${COMPOSE_FILE}" --env-file "${ENV_FILE}" ps
    ;;
  logs)
    "${COMPOSE_CMD[@]}" --file "${COMPOSE_FILE}" --env-file "${ENV_FILE}" logs -f
    ;;
  *)
    usage
    exit 1
    ;;
esac
