# macOS CLI Port: Validation Record and TDD Plan

## Goal

Add a supported macOS command-line workflow to the existing WitchyBND project
without changing Windows behavior or redistributing proprietary Oodle binaries.
The first complete macOS release will use native WitchyBND code for supported
formats and an optional, separately installed Wine backend for Oodle operations.

## Decisions From Final Validation

### Validated behavior

- The unchanged WitchyBND project builds and runs in Linux after initializing
  its pinned submodules.
- The Elden Ring root `regulation.bin` unpacked to 195 files (52 MB), repacked,
  and unpacked again with no extracted-content or path differences.
- Windows WitchyBND runs through Wine and loads the user's
  `oo2core_6_win64.dll`.
- Official Oodle through Wine decoded all six Kraken fixtures tested, ranging
  from 57,796 compressed bytes to 6,872,203 compressed bytes.
- The updated AnimeStudio `ooz` fork builds as a native arm64 executable and
  dylib using SIMDe.
- Open-source `ooz` decompression matched official Oodle byte-for-byte for
  `c0000_a6x.anibnd.dcx` (130,036 bytes decompressed, SHA-256
  `9d35b674bbf1922677de2fab18dd3117048bcdf5b285caee6ac96d61230f1172`).
- Open-source `ooz` compression produced streams that official Oodle decoded
  byte-for-byte for 130,036-byte, 307,636-byte, and 67,960,282-byte inputs.
- Full Witchy extraction of the original and open-source-recompressed
  `c0000.anibnd` produced the same 655 files (66 MB) with no differences.

### Failed or incomplete behavior

- Both the original `rarten/ooz` decoder and the updated native arm64 fork
  failed to decode five of six valid Elden Ring fixtures. The failures include
  `c0000_a00_lo.anibnd.dcx`, which is only 171,339 compressed bytes.
- `ooz` documents that its decoder is not fuzz-safe.
- Wine in an amd64 Docker container on Apple Silicon emitted a Rosetta x86 AVX
  state assertion during one multi-file run, although all requested outputs
  were produced correctly. Host Wine/CrossOver must be tested directly.
- The current CLI requires a terminal unless `--silent` is used.
- In a minimal Linux container without an existing application-data directory,
  configuration initialization can resolve to the executable directory and
  collide with the `WitchyBND` executable name.
- No test performed here can prove that Elden Ring accepts and loads a
  recompressed archive. That requires a Windows game smoke test.

### Resulting architecture

```text
Native macOS WitchyBND CLI
|-- Native formats: existing managed parsers and native zstd
|-- Oodle decompression/recompression: Wine backend + user-owned oo2core DLL
`-- Experimental native Oodle compression: optional ooz backend
```

Do not bundle Wine, CrossOver, Rosetta, or an Oodle DLL. Do not enable the
open-source decoder for general use until it passes the compatibility and
safety gates defined below.

## Test Fixture Policy

- Keep copyrighted game fixtures outside Git or under a gitignored
  `WitchyTests/FixturesLocal` directory.
- Commit only generated, freely redistributable synthetic fixtures when their
  provenance and license are documented.
- Discover local fixtures through `WITCHY_FIXTURES_ROOT`.
- Mark tests requiring game files as `LocalGameFixture` and skip them with an
  explicit reason when the environment variable is absent.
- Store expected hashes and relative path manifests in test source; do not
  store extracted game content in Git.

## TDD Rules Used In Every Phase

1. Add or amend a test that fails for the missing behavior.
2. Implement the smallest production change that makes it pass.
3. Run the focused test set, then the platform-appropriate regression suite.
4. Refactor only while the focused and regression suites remain green.
5. Do not advance past a phase gate with skipped required tests, unexplained
   warnings introduced by the phase, or unreviewed platform-specific branches.

## Phase 0: Reproducible Characterization Harness

Write tests first:

- A fixture locator test that accepts an external root and never copies fixture
  bytes into the repository.
- A SHA-256 and relative-tree comparison helper test.
- A process-runner test that captures exit code, stdout, stderr, cancellation,
  and timeout without requiring an interactive terminal.
- Characterization tests for the current `regulation.bin` and DCX behavior.

Implementation:

- Add reusable test helpers for local fixtures, directory manifests, and CLI
  subprocesses.
- Add scripts that reproduce the Linux and Wine validation without embedding
  machine-specific absolute paths.

Gate:

- `regulation.bin` unpack/repack/unpack yields the same 195-file tree locally.
- The six Kraken fixtures have recorded sizes and official decompressed hashes.
- Missing local fixtures result in an explicit skip, never a false pass.
- Existing Windows and Linux tests remain green.

## Phase 1: Platform and CLI Boundaries

Write tests first:

- Platform detection returns Windows, Linux, and macOS correctly through an
  injectable platform probe.
- Application-data resolution returns a non-empty absolute directory and does
  not overlap the executable path.
- CLI parse mode can initialize without registry, Explorer, shell-link, folder
  picker, or updater access on macOS.
- `--silent` never initializes PromptPlus terminal rendering.

Implementation:

- Remove the assembly-wide Windows support declaration from the CLI entry
  point and annotate only genuinely Windows-specific APIs.
- Put shell integration and Windows self-update behavior behind platform
  interfaces or guarded registrations.
- Resolve macOS configuration under the standard user application-support
  location and harden empty-path fallback behavior on every platform.
- Preserve all existing Windows integration behavior.

Gate:

- Focused platform tests pass for all three simulated platforms.
- A headless macOS CLI invocation of `--help`, `--version`, and `--silent`
  returns without PromptPlus or native-dialog initialization.
- Windows shell-integration tests and Linux CLI tests show no regression.

## Phase 2: Native macOS Build and Non-Oodle Workflows

Write tests first:

- Restore/build tests cover `osx-arm64` and `osx-x64` runtime identifiers.
- Native-library resolution selects the correct zstd dylib per architecture.
- A macOS integration test unpacks `regulation.bin` and compares its manifest.
- DCX DFLT and ZSTD synthetic round trips compare decompressed bytes exactly.

Implementation:

- Add `osx-arm64` and `osx-x64` runtime identifiers and publish directories.
- Exclude Windows shell-extension build and content from macOS publishing.
- Supply or consume zstd through a package with supported macOS native assets.
- Remove NativeFileDialog from the noninteractive macOS execution path.
- Add macOS CI build, unit-test, and publish jobs.

Gate:

- Both macOS RIDs restore, build, test, and publish from a clean checkout.
- The arm64 artifact runs natively (`file` reports arm64, not x86-64).
- `regulation.bin`, DFLT, and ZSTD integration gates pass.
- Published macOS artifacts contain no `WitchyBND.Shell`, Windows registry
  helper, Windows Oodle DLL, or Linux `.so` payload.

## Phase 3: Oodle Backend Contract

Write tests first:

- Backend selection covers unavailable, Wine, licensed-native, and
  experimental-open-source states.
- Decompress and compress contracts validate input/output sizes and reject
  short, oversized, or inconsistent results.
- Cancellation and timeout tests terminate backend processes and clean partial
  outputs.
- Errors name the missing dependency and include a remediation command without
  dumping proprietary paths or binary data.

Implementation:

- Introduce a narrow Oodle backend interface outside parser concerns.
- Adapt SoulsFormats DCX calls through the interface instead of static
  platform-specific P/Invoke selection.
- Keep the existing Windows and Linux native-library adapters as backends.
- Make backend choice explicit in diagnostics and optionally configurable.

Gate:

- Parser tests use a fake backend and contain no platform process assumptions.
- Existing Windows Oodle behavior passes unchanged through the new adapter.
- No backend can return a wrong-sized result as success.

## Phase 4: Wine Compatibility Backend

Write tests first:

- Discovery tests cover `wine`, `wine64`, CrossOver launchers, explicit config,
  missing launchers, and paths containing spaces.
- DLL discovery accepts an explicit user path and validates PE architecture and
  required Oodle exports without copying it into Git-controlled locations.
- End-to-end tests decode all six Kraken fixtures and compare official hashes.
- End-to-end tests repack and unpack the representative `c0000.anibnd` tree.
- Process tests cover nonzero exit, crash, timeout, cancellation, and cleanup.

Implementation:

- Build a CLI-only Windows helper artifact that excludes Explorer integration.
- Invoke the helper through the discovered Wine/CrossOver launcher.
- Use a private cache/work directory with restrictive permissions and atomic
  output replacement.
- Require the user to point to a legally obtained `oo2core_*_win64.dll`.
- Add `witchybnd doctor` output for Wine, helper, DLL, and architecture status.

Gate:

- All six fixtures decode to their official hashes on an Intel Mac or an Apple
  Silicon Mac with a supported Wine/CrossOver configuration.
- Full extraction of `c0000.anibnd.dcx` produces the recorded 655-file tree.
- Wine is optional for non-Oodle commands and is never downloaded implicitly.
- No proprietary DLL appears in package contents, logs, caches intended for
  distribution, or Git status.

## Phase 5: Experimental Native Ooz Compression

Write tests first:

- Native arm64 and x86-64 builds export the bounded compression wrapper API.
- Compression tests cover the 130,036-byte, 307,636-byte, and 67,960,282-byte
  inputs from validation.
- Official Oodle under the Wine test backend decodes each generated stream to
  the exact original hash.
- Buffer-bound, allocation-failure, cancellation, and concurrency tests fail
  closed.

Implementation:

- Vendor or submodule a reviewed, pinned GPLv3-compatible `ooz` source revision
  with its license and provenance recorded.
- Expose only the required compression API through a small C ABI wrapper.
- Build universal or per-RID dylibs with SIMDe and hidden symbols by default.
- Keep the feature opt-in until the in-game gate passes.

Gate:

- Official Oodle decodes every generated test stream byte-for-byte.
- Full Witchy extraction of original and recompressed `c0000.anibnd` remains
  identical across all 655 files.
- A recompressed archive loads successfully in Elden Ring on Windows and the
  affected animation can be exercised in game.
- Failure of this phase does not block the Wine-backed release.

## Phase 6: Open-Source Decompression Research (Non-Blocking)

Write tests first:

- Preserve the six-fixture compatibility matrix as a mandatory test suite.
- Add malformed, truncated, oversized, and mutation-generated inputs executed
  in an isolated helper process.
- Add memory and time ceilings for each fixture.

Implementation:

- Investigate the five valid-stream failures in the updated `ooz` decoder.
- Keep decoding out of the main process because upstream states it is not
  fuzz-safe.
- Consider a sandboxed helper even after compatibility fixes.

Gate:

- All six official fixtures decode to exact reference hashes.
- Fuzz and sanitizer runs complete with no crashes, out-of-bounds access,
  hangs, or unbounded allocation.
- Compatibility extends beyond Elden Ring to at least one fixture for each
  Oodle generation WitchyBND claims to support.
- Until every gate passes, the open-source decoder is unavailable in release
  builds and Wine remains the Oodle decompression backend.

## Phase 7: Packaging, Documentation, and Release

Write tests first:

- Artifact inspection tests verify architecture, executable permissions,
  dependency closure, package contents, and absence of proprietary files.
- Clean-machine smoke tests cover no Wine, Wine without DLL, and complete Wine
  configuration.
- Upgrade tests preserve configuration and do not modify game installations.

Implementation:

- Publish signed/notarized macOS arm64 and x86-64 tarballs.
- Add a Homebrew formula or tap only after notarized artifacts are stable.
- Document native capabilities, Wine setup, user-supplied DLL setup, security
  boundaries, and exact unsupported cases.
- Disable automatic self-update on macOS until it has a separately tested,
  signed, atomic update mechanism.

Gate:

- Clean Intel and Apple Silicon machines pass install, doctor, regulation,
  Oodle decode, repack, and uninstall smoke tests.
- Gatekeeper accepts the distributed binaries without bypass instructions.
- Release artifacts are reproducible enough to produce matching managed
  assemblies and documented native-build provenance.
- Windows and Linux release jobs remain green.

## Release Definition of Done

- Native macOS CLI works for all non-Oodle formats covered by existing tests.
- Oodle operations work through an explicitly configured Wine/CrossOver
  backend and a user-owned DLL.
- No proprietary runtime is redistributed.
- Every automated gate above is green, except the explicitly non-blocking
  open-source decompression research phase.
- A human Windows game test confirms that a representative recompressed
  animation archive loads and behaves correctly in Elden Ring.
- Remaining limitations are stated in release notes without implying native
  Oodle decompression support that the compatibility matrix does not justify.

## Execution Record (2026-07-11)

The local validation host is an Apple Silicon M3 Mac (`arm64`), not an Intel
Mac. Any `osx-x64` result recorded below is a cross-build result only unless it
is explicitly attributed to a clean Intel machine.

### Phase status

| Phase | Automated TDD gate | Release gate | Status |
| --- | --- | --- | --- |
| 0. Characterization | External fixtures, manifests, process runner, regulation and Kraken characterization | Windows/Linux regression | Automated gate passed |
| 1. Platform/CLI | Simulated platform boundaries and noninteractive CLI tests | Windows shell behavior unchanged | Automated gate passed; Windows CI remains authoritative |
| 2. Native macOS | Both RIDs build/publish; arm64 execution, DFLT/ZSTD, regulation and artifact inspection | Execute x64 artifact on clean Intel Mac | Arm64 gate passed; Intel execution outstanding |
| 3. Oodle contract | Backend selection, bounds, diagnostics, cancellation and cleanup | Existing Windows Oodle path unchanged | Automated gate passed; Windows CI remains authoritative |
| 4. Wine backend | DLL/PE validation, helper protocol, six fixtures and 655-file manifest | Supported host Wine/CrossOver on clean Apple Silicon and Intel Macs | Container automation passed; clean-host rows outstanding |
| 5. Native ooz compression | Three input sizes decoded byte-for-byte by official Oodle; full 655-file CLI round trip | Load recompressed archive and exercise animation in Elden Ring on Windows | Automated gate passed; human game gate outstanding |
| 6. Decoder research | Isolated six-fixture compatibility characterization | All compatibility, fuzz and sanitizer gates before release exposure | Research result is 1/6; decoder remains disabled as designed |
| 7. Release | CI, artifact inspection, docs, provenance and no-proprietary-payload checks | Developer ID signing, notarization, Gatekeeper and both clean-machine rows | Implementation complete; release qualification outstanding |

No Homebrew formula is to be published until every Phase 7 release gate and
the Phase 5 Windows game gate pass against the same notarized artifacts.

### Automated gates passed

- Native `osx-arm64` restore and build completed with .NET SDK 10.0.101.
- The focused platform, process, diagnostics, atomic-write, and Oodle backend
  suite passed 41/41 tests with no skips.
- The complete macOS arm64 regression suite passed 133/133 tests with no
  skips (`TestCategory!=SkipOnGitHubAction`). This was rerun after the final
  private-work-directory permissions change and before the final publish.
- The Linux amd64 regression suite passed 128/128 tests with no skips in the
  supported cross-platform categories.
- The published executable and zstd dependency are Mach-O arm64 binaries.
- Published `--help`, `--version`, `--silent --version`, and `--doctor` are
  noninteractive; silent version output is empty.
- Artifact inspection found no `WitchyBND.Shell`, Linux `.so`, or proprietary
  `oo2core` payload. The bundled Wine helper is one 19 KB native x64 PE and
  contains no Oodle DLL or managed runtime.
- Native unpack/repack/unpack of the user's Elden Ring root `regulation.bin`
  produced identical SHA-256 manifests containing 195 files.
- The native Windows helper decoded all six recorded Kraken fixtures through
  official Oodle. `LocalOodleFixtureTests` passed 6/6 with external fixtures
  and explicit Wine/helper/DLL configuration.
- The pinned native compressor produced official-Oodle-compatible streams for
  130,036-byte, 307,636-byte, and 67,960,282-byte real inputs. Original and
  recompressed full `c0000` Witchy extractions had identical 655-file
  manifests.
- The published CLI unpacked full `c0000.anibnd.dcx` through the Wine backend,
  repacked its extracted tree with `--oodle-native-compression`, and unpacked
  the result through official Oodle with the same 655-file manifest.
- Compression levels above 4 are clamped inside the native ABI after a pure-C
  reproduction proved the pinned upstream level-6 path crashes on the 68 MB
  fixture. Official Oodle decoded the clamped stream byte-for-byte.
- Binder output writes use sibling temporary files and atomic replacement;
  focused tests prove failed writes preserve the prior destination.
- The unsafe research decoder remains absent from release exports. Its
  isolated opt-in matrix reproduces the expected one success and five failures.
- `dev/validate-macos-artifact.sh` and
  `dev/validate-regulation-roundtrip.sh` reproduce the native release gates.
  `dev/validate-oodle-backends.sh` reproduces the official three-size and
  655-file gates; `dev/validate-ooz-decoder-research.sh` preserves the 1/6
  decoder characterization outside release builds.
- The final arm64 publish was regenerated from the tested revision, then both
  artifact inspection and the user's 195-file root `regulation.bin` round trip
  passed again. Shell syntax, workflow YAML, `git diff --check`, submodule
  cleanliness, and the proprietary-payload scan also passed.
- CI now restores, builds, tests, publishes, archives, and uploads both
  `osx-arm64` on `macos-15` and `osx-x64` on `macos-15-intel`; both RID
  lock-file sets are present and each runner supplies its native zstd dylib.
- CI builds the Wine helper once with MinGW, downloads that single PE into both
  Mac jobs, validates package dependency closure, and requires Developer ID
  signing and Apple notarization for main-branch release artifacts.
- The SoulsFormats backend hook is published at
  `guyathomas/SoulsFormatsNEXT@ee0bc5eae190275fcd85e8fbb203ee58bba02fd5`;
  `.gitmodules` and the parent gitlink point to that clean dependency commit.

### Gates intentionally not claimed

- The complete automated Wine matrix passed through the amd64 Wine container,
  including invocation from the native macOS CLI via the validation launcher.
  A clean-machine run with a supported host Wine/CrossOver installation is
  still required for release qualification.
- The x86-64 artifact is covered by the CI matrix but has not been executed on
  an Intel Mac in this environment.
- The experimental open-source decoder remains disabled because it passed only
  one of six valid Elden Ring streams. This is the expected Phase 6 outcome.
- Native open-source compression is included as an explicit opt-in backend;
  open-source decompression remains disabled. The Phase 5 automated gates pass,
  but the Windows in-game gate remains outstanding.
- Signing, notarization, Gatekeeper validation, Homebrew distribution, and
  clean Intel/Apple Silicon machine tests require release credentials or
  external machines and remain Phase 7 release gates.
- A human must load a representative recompressed archive in Elden Ring on
  Windows and exercise the affected animation before release.

### External release evidence still required

Record the following evidence in this file or an attached release record before
changing the corresponding phase status to passed:

1. Apple Silicon clean machine: model, macOS version, Wine/CrossOver version,
   artifact SHA-256, checklist command exit codes, `spctl` result, and
   notarization submission ID.
2. Intel clean machine: the same evidence, including native x86-64 execution.
3. Windows Elden Ring: game version, archive SHA-256, target animation, install
   method, successful load, and observed in-game animation result.
4. CI release: successful Windows and Linux jobs plus both signed/notarized Mac
   jobs using a Developer ID Application identity. The current machine has only
   an Apple Development identity, which is not sufficient for distribution.

### Six-fixture official matrix

| Fixture | Compressed | Decompressed | SHA-256 |
| --- | ---: | ---: | --- |
| `c0000.anibnd` | 6,872,203 | 67,960,282 | `1e9ccff8d91ae07f57faa8a94a2f3a9cc10a5f38bb0d8b1db2c05facd82424aa` |
| `c0000_a00_hi.anibnd` | 12,190,426 | 21,557,252 | `e65ae56a4f08190bb8e3679526df470178cc8a8e4fdf892a9737dbd8e4b721c7` |
| `c0000_a00_md.anibnd` | 1,038,460 | 1,715,364 | `d8215940de5b1fd55914fa28d076cfa8dac7474098d3859f25e42dd73f6d7d01` |
| `c0000_a00_lo.anibnd` | 171,339 | 307,636 | `64e9891c0eb350668c2efa2cf0a2def68919586e412598c8d85f94b30d92c0da` |
| `c0000_a0x.anibnd` | 2,543,147 | 5,028,276 | `184fe08ea4a18a6aa5edc17a4747c5428e651ff870b48c030f20cdf39b58d866` |
| `c0000_a6x.anibnd` | 57,796 | 130,036 | `9d35b674bbf1922677de2fab18dd3117048bcdf5b285caee6ac96d61230f1172` |
