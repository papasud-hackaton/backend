#!/usr/bin/env bash
# Buildea la imagen de producción y la publica en el registry del VPS.
#
# Uso:
#   ./scripts/publish-image.sh            # tag "test"
#   ./scripts/publish-image.sh beta       # tag "beta"
#   ./scripts/publish-image.sh v1.2.3     # cualquier tag
#
# El puerto 5000 del VPS está cerrado por el firewall del proveedor, así que el
# script detecta si el registry es accesible:
#   - accesible  -> docker push directo (rápido)
#   - cerrado    -> manda la imagen por SSH y la pushea desde adentro del server
#
# SSH: si tu clave no es la default, pasala con SSH_KEY=~/.ssh/mi_clave, o dejá
# configurado el host en ~/.ssh/config:
#
#   Host papasur-vps
#     HostName 2.24.86.109
#     User root
#     IdentityFile ~/.ssh/tu_clave
set -euo pipefail

REGISTRY_HOST="${REGISTRY_HOST:-2.24.86.109}"
REGISTRY_PORT="${REGISTRY_PORT:-5000}"
SSH_USER="${SSH_USER:-root}"
SSH_TARGET="${SSH_USER}@${REGISTRY_HOST}"
SSH_KEY="${SSH_KEY:-}"

ssh_opts=()
if [[ -n "$SSH_KEY" ]]; then
  ssh_opts=(-i "$SSH_KEY")
fi

remote() { ssh "${ssh_opts[@]}" "$SSH_TARGET" "$@"; }
TAG="${1:-test}"

REGISTRY="${REGISTRY_HOST}:${REGISTRY_PORT}"
IMAGE="${REGISTRY}/papasur/api:${TAG}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$REPO_ROOT"

echo "==> Build ${IMAGE} (linux/amd64, target final)"
DOCKER_REGISTRY="$REGISTRY" API_IMAGE_TAG="$TAG" \
  docker compose -f docker-compose.yml build

if curl -sf -m 5 "http://${REGISTRY}/v2/" >/dev/null 2>&1; then
  echo "==> Registry accesible: push directo"
  echo "    (si falla por TLS, agregá \"${REGISTRY}\" a insecure-registries en Docker)"
  DOCKER_REGISTRY="$REGISTRY" API_IMAGE_TAG="$TAG" \
    docker compose -f docker-compose.yml push
else
  echo "==> Registry NO accesible desde acá (firewall). Publicando vía SSH."
  echo "==> Transfiriendo imagen a ${SSH_TARGET} (puede tardar unos minutos)"
  docker save "$IMAGE" | gzip -1 | remote 'gunzip | docker load'

  echo "==> Push al registry local del server"
  remote \
    "docker tag '${IMAGE}' 'localhost:${REGISTRY_PORT}/papasur/api:${TAG}' \
     && docker push 'localhost:${REGISTRY_PORT}/papasur/api:${TAG}'"
fi

echo "==> Listo. Tags publicados:"
remote "curl -s localhost:${REGISTRY_PORT}/v2/papasur/api/tags/list"
echo
echo "En Portainer: Stacks -> papasur -> Update the stack -> marcar 'Re-pull image'."
