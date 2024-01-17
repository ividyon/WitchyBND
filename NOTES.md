**Note**: This release comes with updates to the context menu. Please see the [notes on updating Witchy](https://github.com/ividyon/WitchyBND?tab=readme-ov-file#updating-witchybnd) in the README. 

This release introduces several new features that greatly enhance the comfort of working with certain types of mod files. As an example, you can combine the File Watcher, Recursive and "Process to..." features to edit raw weapon files in a completely separate directory, have any changes directly repacked into a .partsbnd.dcx file in the mod, which lets you directly see the changes ingame without additional actions and without cluttering the mod directory.

Various other fixes and small additions should make it less frequent to encounter issues during usage. And if they do happen, they should provide meaningful output.

* Added **File Watcher** mode.
  * When selected, Witchy watches for changes to the selected files (or unpacked BND folders). When changes happen, the affected files is automatically processed.
  * Can be combined with the "Recursive" setting and the "Process to..." path selection for some dramatic results in editing comfort.
* Added an online update check. Witchy will now notify you if there are new versions available.
* Added an "Offline mode" configuration option which disables any connectivity to the internet, such as the update check.
  * This is disabled by default (meaning the update check will run).
* Expanded the Recursive setting to work when repacking binders, as well.
  * Witchy will check if any of the files to be packed into the binder are unpacked, and will repack them before adding the file to the binder.
  * Example use case: Repacking a PARTSBND would first repack the TPF inside when using Recursive, thus applying any texture changes.
* Witchy will now attempt to automatically fetch the Oodle DLL from your detected Steam game folders.
* Added `entryfilelist` serialization support.
* Added the "Watch for changes" option to the Witchy context menu, and cleaned up its structure.
* Slightly optimized the Preprocess step to avoid going through parsers unnecessarily after a match.
* Enabled parallel processing by default. It can be disabled in the settings by launching the WitchyBND executable.
* Fixed any known issues with file list ordering during parallel processing.
* Fixed an issue where text would become jumbled during parallel processing.
* Fixed an issue with exception handling where the application would not pause on an unhandled exception. 