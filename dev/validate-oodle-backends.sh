#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 4 || $# -gt 5 ]]; then
  echo "usage: validate-oodle-backends.sh OODLE_DLL C0000_DCX A00_DCX A6X_DCX [WITCHY]" >&2
  exit 2
fi

root=$(cd "$(dirname "$0")/.." && pwd)
oodle_dll=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
c0000=$(cd "$(dirname "$2")" && pwd)/$(basename "$2")
a00=$(cd "$(dirname "$3")" && pwd)/$(basename "$3")
a6x=$(cd "$(dirname "$4")" && pwd)/$(basename "$4")
witchy=${5:-}
work=$(mktemp -d "${TMPDIR:-/tmp}/witchy-oodle.XXXXXX")
trap 'rm -rf "$work"' EXIT

mkdir -p "$work/helper" "$work/wp"
"$root/WitchyOodleHelper.Native/build.sh" "$work/helper/WitchyOodleHelper.exe"
cmake -S "$root/third_party/ooz" -B "$work/ooz-build" \
  -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="$(uname -m)" >/dev/null
cmake --build "$work/ooz-build" --parallel >/dev/null
clang++ -std=c++17 "$root/dev/ooz-wrapper-smoke.cpp" \
  -L"$work/ooz-build" -lwitchy_ooz -Wl,-rpath,"$work/ooz-build" \
  -o "$work/ooz-wrapper-smoke"
cp "$oodle_dll" "$work/oo2core.dll"

validate_stream() {
  local source=$1
  local name=$2
  local sizes
  sizes=$(ruby -e 'd=File.binread(ARGV[0],36); abort "not DCX" unless d.start_with?("DCX\0"); puts [d.byteslice(28,4).unpack1("N"),d.byteslice(32,4).unpack1("N")].join(" ")' "$source")
  local raw_size=${sizes%% *}
  local compressed_size=${sizes##* }
  dd if="$source" of="$work/$name.original.krak" bs=1 skip=76 count="$compressed_size" status=none

  docker run --rm --platform linux/amd64 \
    -e WINEPREFIX=/work/wp -e WINEDEBUG=-all \
    -v "$work:/work" oodle-wine:latest bash -lc \
    "wine /work/helper/WitchyOodleHelper.exe 6 /work/oo2core.dll decompress /work/$name.original.krak /work/$name.original.raw $raw_size"
  "$work/ooz-wrapper-smoke" "$work/$name.original.raw" "$work/$name.native.krak"
  docker run --rm --platform linux/amd64 \
    -e WINEPREFIX=/work/wp -e WINEDEBUG=-all \
    -v "$work:/work" oodle-wine:latest bash -lc \
    "wine /work/helper/WitchyOodleHelper.exe 6 /work/oo2core.dll decompress /work/$name.native.krak /work/$name.native.raw $raw_size"
  cmp "$work/$name.original.raw" "$work/$name.native.raw"
  shasum -a 256 "$work/$name.native.raw"
}

validate_stream "$c0000" c0000
validate_stream "$a00" a00
validate_stream "$a6x" a6x

if [[ -n "$witchy" ]]; then
  mkdir "$work/original-tree" "$work/native-tree"
  cp "$work/c0000.original.raw" "$work/original-tree/c0000.anibnd"
  cp "$work/c0000.native.raw" "$work/native-tree/c0000.anibnd"
  "$witchy" --silent --passive "$work/original-tree/c0000.anibnd"
  "$witchy" --silent --passive "$work/native-tree/c0000.anibnd"
  (cd "$work/original-tree/c0000-anibnd-wanibnd" && find . -type f -print0 | sort -z | xargs -0 shasum -a 256) > "$work/original.manifest"
  (cd "$work/native-tree/c0000-anibnd-wanibnd" && find . -type f -print0 | sort -z | xargs -0 shasum -a 256) > "$work/native.manifest"
  test "$(wc -l < "$work/original.manifest" | tr -d ' ')" = 655
  cmp "$work/original.manifest" "$work/native.manifest"
fi
