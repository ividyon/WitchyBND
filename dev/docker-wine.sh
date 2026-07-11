#!/usr/bin/env bash
set -euo pipefail

prefix=${WITCHY_DOCKER_WINE_PREFIX:-/tmp/witchy-docker-wine-prefix}
mkdir -p "$prefix"

docker run --rm --platform linux/amd64 \
  -e WINEPREFIX="$prefix" \
  -e WINEDEBUG=-all \
  -v /Users:/Users \
  -v /tmp:/tmp \
  oodle-wine:latest wine "$@"
