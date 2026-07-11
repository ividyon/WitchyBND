#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "usage: sign-notarize-macos.sh ARTIFACT_DIRECTORY NOTARY_ZIP" >&2
  exit 2
fi
: "${APPLE_SIGNING_IDENTITY:?set APPLE_SIGNING_IDENTITY}"

artifact=$(cd "$1" && pwd)
notary_zip=$2

codesign --force --timestamp --options runtime --sign "$APPLE_SIGNING_IDENTITY" \
  "$artifact/libzstd.dylib"
codesign --force --timestamp --options runtime --sign "$APPLE_SIGNING_IDENTITY" \
  "$artifact/libwitchy_ooz.dylib"
codesign --force --timestamp --options runtime --sign "$APPLE_SIGNING_IDENTITY" \
  "$artifact/WitchyBND"
codesign --verify --strict --verbose=2 "$artifact/WitchyBND"

rm -f "$notary_zip"
ditto -c -k --keepParent "$artifact" "$notary_zip"

if [[ -n ${APPLE_NOTARY_PROFILE:-} ]]; then
  xcrun notarytool submit "$notary_zip" --keychain-profile "$APPLE_NOTARY_PROFILE" --wait
else
  : "${APPLE_ID:?set APPLE_ID or APPLE_NOTARY_PROFILE}"
  : "${APPLE_TEAM_ID:?set APPLE_TEAM_ID or APPLE_NOTARY_PROFILE}"
  : "${APPLE_APP_PASSWORD:?set APPLE_APP_PASSWORD or APPLE_NOTARY_PROFILE}"
  xcrun notarytool submit "$notary_zip" \
    --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_APP_PASSWORD" --wait
fi
