**Note**: This release comes with updates to the context menu.
Read the [notes on updating Witchy](https://github.com/ividyon/WitchyBND?tab=readme-ov-file#updating-witchybnd) to learn how to properly update the context menu. 

* Added **File Watcher** mode.
  * When selected, Witchy watches for changes to the selected files (or unpacked BND folders). When changes happen, the affected files is automatically processed.
  * Can be combined with the "Recursive" setting and the "Process to..." path selection for some dramatic results in editing comfort.
* Added an online update check. Witchy will now notify you if there are new versions available.
* Added an "Offline mode" configuration option which disables any connectivity to the internet, such as the update check.
  * This is disabled by default.
* Expanded the Recursive setting to work when repacking binders, as well.
  * Witchy will check if any of the files to be packed into the binder are unpacked, and will repack them before adding the file to the binder.
  * Example use case: Repacking a PARTSBND would first repack the TPF inside when using Recursive, thus applying any texture changes.
* Witchy will now attempt to automatically fetch the oo2core DLL from your detected Steam game folders.
* Added `entryfilelist` serialization support.
* Cleaned up the context menu and added the File Watcher mode to it.
* Fixed known issues with file list ordering during parallel processing.
* Enabled parallel processing by default. It can be disabled in the settings by launching the WitchyBND executable.
* Fixed an issue where text would become jumbled during parallel processing.
* Slightly optimized the Preprocess step to avoid going through parsers unnecessarily after a match.