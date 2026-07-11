#!/usr/bin/env bash
set -euo pipefail

artifact=${1:?usage: validate-macos-artifact.sh ARTIFACT_DIRECTORY}
executable="$artifact/WitchyBND"

test -x "$executable"
file "$executable"
file "$artifact/libzstd.dylib"
file "$artifact/libwitchy_ooz.dylib"
test -f "$artifact/Tools/WitchyOodleHelper.exe"
test "$(find "$artifact/Tools" -type f | wc -l | tr -d ' ')" = 1
file "$artifact/Tools/WitchyOodleHelper.exe" | grep -q 'PE32+.*x86-64'

while IFS= read -r binary; do
  first_dependency_line=2
  [[ $binary = *.dylib ]] && first_dependency_line=3
  while IFS= read -r dependency; do
    case "$dependency" in
      /System/*|/usr/lib/*|@rpath/*|@loader_path/*|@executable_path/*) ;;
      *) echo "unexpected Mach-O dependency: $binary -> $dependency" >&2; exit 1 ;;
    esac
  done < <(otool -L "$binary" | tail -n +"$first_dependency_line" | awk '{print $1}')
done < <(find "$artifact" -type f \( -name '*.dylib' -o -perm -111 \) -exec file {} \; | awk -F: '/Mach-O/{print $1}')

if [[ ${REQUIRE_SIGNED:-0} = 1 ]]; then
  codesign --verify --strict --verbose=2 "$executable"
  authority=$(codesign -dv --verbose=2 "$executable" 2>&1 | sed -n 's/^Authority=//p' | head -1)
  test -n "$authority"
fi

exports=$(nm -gU "$artifact/libwitchy_ooz.dylib")
grep -q 'WitchyOoz_Compress' <<<"$exports"
grep -q 'WitchyOoz_MaxCompressedSize' <<<"$exports"
if grep -q 'Ooz_Decompress' <<<"$exports"; then
  echo "experimental decoder must not be exported" >&2
  exit 1
fi

if find "$artifact" -type f \( -name 'WitchyBND.Shell*' -o -name '*.so' -o -iname 'oo2core*.dll' \) | grep -q .; then
  echo "artifact contains a forbidden platform or proprietary payload" >&2
  exit 1
fi

"$executable" --help >/dev/null
"$executable" --version >/dev/null
test -z "$("$executable" --silent --version)"
