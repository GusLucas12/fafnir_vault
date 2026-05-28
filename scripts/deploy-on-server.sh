#!/usr/bin/env bash
set -euo pipefail

DEPLOY_BRANCH="${DEPLOY_BRANCH:-main}"

if [[ -z "${DEPLOY_PATH:-}" ]]; then
  DEPLOY_PATH="$(pwd)"
fi

cd "$DEPLOY_PATH"

if [[ ! -f ".env" ]]; then
  echo "Arquivo .env nao encontrado em $DEPLOY_PATH" >&2
  exit 1
fi

git fetch origin "$DEPLOY_BRANCH"
git checkout "$DEPLOY_BRANCH"
git pull --ff-only origin "$DEPLOY_BRANCH"

docker compose pull || true
docker compose up -d --build --remove-orphans
