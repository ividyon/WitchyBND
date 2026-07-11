#!/usr/bin/env bash
set -euo pipefail

output=${1:-bin/helper/win-x64/WitchyOodleHelper.exe}
compiler=${MINGW_CC:-x86_64-w64-mingw32-gcc}
mkdir -p "$(dirname "$output")"
"$compiler" -std=c17 -O2 -Wall -Wextra -Werror -Wno-cast-function-type \
  -static-libgcc -s WitchyOodleHelper.Native/main.c -o "$output"
