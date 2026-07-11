# macOS CLI release checklist

## Required CI secrets

- `MACOS_CERTIFICATE`: base64-encoded Developer ID Application `.p12`
- `MACOS_CERTIFICATE_PASSWORD`: password for that `.p12`
- `MACOS_KEYCHAIN_PASSWORD`: temporary CI keychain password
- `APPLE_SIGNING_IDENTITY`: full Developer ID Application identity
- `APPLE_ID`: notarization Apple ID
- `APPLE_TEAM_ID`: Apple developer team ID
- `APPLE_APP_PASSWORD`: app-specific notarization password

Main-branch macOS jobs intentionally fail if these are absent. Pull-request and
non-main development artifacts remain ad-hoc signed and are not releases.

## Clean-machine matrix

Run on both an Apple Silicon Mac and an Intel Mac:

1. Extract the matching release tarball into a new directory.
2. Run `REQUIRE_SIGNED=1 dev/validate-macos-artifact.sh <directory>` from a
   source checkout containing the validation script.
3. Run `spctl --assess --type execute --verbose=4 <directory>/WitchyBND`.
4. Run `<directory>/WitchyBND --help`, `--version`, and `--doctor` without
   Wine installed. No command may prompt or crash.
5. Run `dev/validate-regulation-roundtrip.sh` against a copied root
   `regulation.bin`.
6. Install a supported Wine or CrossOver build. Do not copy it into the Witchy
   directory.
7. Set `WITCHY_WINE`, `WITCHY_OODLE_LIBRARY`, and, if needed,
   `WITCHY_OODLE_HELPER`; run the six `LocalOodleFixtureTests`.
8. Run `dev/validate-oodle-backends.sh` against `c0000`, `c0000_a00_lo`, and
   `c0000_a6x` and pass the published Witchy executable as the final argument.
9. Remove the extracted directory and confirm no game files were changed and
   no proprietary DLL was copied into the release directory.

Record machine model, macOS version, CPU architecture, Wine/CrossOver version,
artifact SHA-256, all command exit codes, and the notarization submission ID.

## Distribution

Create a Homebrew formula only after both clean-machine rows and the Windows
game smoke test pass against the same notarized artifact. Pin formula URLs and
SHA-256 values to the immutable GitHub release assets.
