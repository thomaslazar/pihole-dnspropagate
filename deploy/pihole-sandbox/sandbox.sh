#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.yaml"
ENV_FILE="${SCRIPT_DIR}/.env"

usage() {
  cat <<USAGE
Usage: $(basename "$0") <up|down|status|logs>

Commands:
  up      Launch the Pi-hole sandbox in detached mode (primary + secondary).
  down    Stop the sandbox and remove containers and volumes.
  status  Show current container state.
  logs    Tail all sandbox container logs (Ctrl+C to exit).

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

load_env() {
  # shellcheck disable=SC1090
  source "${ENV_FILE}"
}

wait_for_container() {
  local container=$1
  local attempts=0
  local max_attempts=30
  while (( attempts < max_attempts )); do
    if ! docker ps --format '{{.Names}}' | grep -qx "$container"; then
      sleep 2
      ((attempts++))
      continue
    fi

    local health
    health=$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$container")
    case "$health" in
      healthy|starting|running)
        return 0
        ;;
    esac
    sleep 2
    ((attempts++))
  done
  echo "Container ${container} did not become healthy in time." >&2
  return 1
}

set_password() {
  local container=$1
  local password=$2
  [[ -z "$password" ]] && return 0

  if wait_for_container "$container"; then
    docker exec "$container" bash -c "pihole setpassword '$password'" >/dev/null
  else
    echo "Skipping password initialization for ${container}." >&2
  fi
}

initialize_passwords() {
  set_password "pihole-primary" "${PRIMARY_PIHOLE_PASSWORD:-}"
  set_password "pihole-secondary" "${SECONDARY_PIHOLE_PASSWORD:-}"
}

cmd=${1:-}
if [[ -z "${cmd}" ]]; then
  usage
  exit 1
fi

ensure_env
ensure_compose
load_env

case "${cmd}" in
  up)
    "${COMPOSE_CMD[@]}" --file "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d
    initialize_passwords
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
