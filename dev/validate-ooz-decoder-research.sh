#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: validate-ooz-decoder-research.sh FIXTURE_ROOT" >&2
  exit 2
fi

root=$(cd "$(dirname "$0")/.." && pwd)
fixtures=$(cd "$1" && pwd)
work=$(mktemp -d "${TMPDIR:-/tmp}/witchy-ooz-research.XXXXXX")
trap 'rm -rf "$work"' EXIT

cmake -S "$root/third_party/ooz" -B "$work/build" \
  -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="$(uname -m)" \
  -DWITCHY_BUILD_OOZ_RESEARCH=ON >/dev/null
cmake --build "$work/build" --target witchy_ooz_research --parallel >/dev/null

validate() {
  local relative=$1
  local expected=$2
  local expected_hash=$3
  local name=$(basename "$relative" .dcx)
  local source="$fixtures/$relative"
  local sizes
  sizes=$(ruby -e 'd=File.binread(ARGV[0],36); puts [d.byteslice(28,4).unpack1("N"),d.byteslice(32,4).unpack1("N")].join(" ")' "$source")
  local raw_size=${sizes%% *}
  local compressed_size=${sizes##* }
  ruby -e 'print [ARGV[0].to_i].pack("Q<")' "$raw_size" > "$work/$name.ooz"
  dd if="$source" bs=1 skip=76 count="$compressed_size" status=none >> "$work/$name.ooz"

  if "$work/build/witchy_ooz_research" -d -f "$work/$name.ooz" "$work/$name.raw" >/dev/null 2>&1; then
    test "$expected" = pass
    test "$(shasum -a 256 "$work/$name.raw" | awk '{print $1}')" = "$expected_hash"
  else
    test "$expected" = fail
  fi
}

validate chr/c0000.anibnd.dcx fail -
validate chr/c0000_a00_hi.anibnd.dcx fail -
validate chr/c0000_a00_md.anibnd.dcx fail -
validate chr/c0000_a00_lo.anibnd.dcx fail -
validate chr/c0000_a0x.anibnd.dcx fail -
validate chr/c0000_a6x.anibnd.dcx pass 9d35b674bbf1922677de2fab18dd3117048bcdf5b285caee6ac96d61230f1172
