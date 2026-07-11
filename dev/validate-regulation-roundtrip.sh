#!/usr/bin/env bash
set -euo pipefail

witchy=${1:?usage: validate-regulation-roundtrip.sh WITCHY REGULATION_BIN}
regulation=${2:?usage: validate-regulation-roundtrip.sh WITCHY REGULATION_BIN}
work=$(mktemp -d "${TMPDIR:-/tmp}/witchy-regulation.XXXXXX")
trap 'rm -rf "$work"' EXIT

cp "$regulation" "$work/regulation.bin"
"$witchy" --silent --passive "$work/regulation.bin"

manifest() {
  (cd "$1" && find . -type f -print0 | sort -z | xargs -0 shasum -a 256)
}

manifest "$work/regulation-bin" > "$work/before.sha256"
test "$(wc -l < "$work/before.sha256" | tr -d ' ')" = 195
"$witchy" --silent --passive "$work/regulation-bin"
mv "$work/regulation-bin" "$work/regulation-bin-first"
"$witchy" --silent --passive "$work/regulation.bin"
manifest "$work/regulation-bin" > "$work/after.sha256"
cmp "$work/before.sha256" "$work/after.sha256"
