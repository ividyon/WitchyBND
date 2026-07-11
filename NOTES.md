* Removed the warnings from the PARAM and TAE parsers, as they have been in personal use for years without major issues.
* Updated the TAE parser slightly. It will now assign group indices to events, rather than event indices to groups, to make the XML way more easily editable. This updates the TAE parser version.
* Re-added GPARAM support, now including Armored Core VI.
* Unpacking a DS2 FFXBND will now fall back on the default BND4 parser, as they differ too much from FFXBNDs from BB onward.
* Made the file watcher mode more robust; it will now retry processing a change up to 5 times while a file is used by another process.
* Made the configuration load more robust; it will now individually test and recreate any corrupted configuration files.
* Added native command-line releases for macOS arm64 and x86-64. Non-Oodle formats, including Elden Ring `regulation.bin`, run without Wine.
* Added an explicit Wine/CrossOver Oodle backend. Users must provide their own legally obtained `oo2core_6_win64.dll`, `oo2core_8_win64.dll`, or `oo2core_9_win64.dll`; no proprietary DLL or Wine runtime is bundled or downloaded.
* Replaced the Wine backend's managed helper with a single native x64 Windows helper and added `--doctor` dependency, architecture, and Oodle-export diagnostics.
* Added opt-in `--oodle-native-compression` using a pinned open-source compressor. Levels above 4 are clamped to 4 because higher levels are unstable on validated large Elden Ring inputs.
* Open-source Oodle decompression remains disabled: the research decoder matched only one of six valid Elden Ring fixtures and is not fuzz-safe. Wine/CrossOver remains required for Oodle decompression.
* macOS folder pickers, Explorer integration, and automatic self-update are unavailable. macOS release artifacts are signed and notarized by CI when release credentials are configured.
