**Note**: This release comes with updates to the context menu.
Read the [notes on updating Witchy](https://github.com/ividyon/WitchyBND?tab=readme-ov-file#updating-witchybnd) to learn how to properly update the context menu. 

This release introduces several new features that greatly enhance the comfort of working with certain types of mod files.

[Practical example of "live editing" assets for a custom weapon](https://cdn.discordapp.com/attachments/1185279928187486309/1196759292125130812/2024-01-16_11-12-30.mp4?ex=65b8cbf1&is=65a656f1&hm=c07b3589d48b55d1326f096697f36e53d91e9bc611448884248bfc569bf0afd9&)

The above was done using the File Watcher, Recursive and "Process to..." features to edit raw weapon files in a completely separate directory, and directly see the changes ingame without having to manually repack several archives every time.

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
* Witchy will now attempt to automatically fetch the oo2core DLL from your detected Steam game folders.
* Added `entryfilelist` serialization support.
* Cleaned up the context menu and added the File Watcher mode to it.
* Fixed any known issues with file list ordering during parallel processing.
* Enabled parallel processing by default. It can be disabled in the settings by launching the WitchyBND executable.
* Fixed an issue where text would become jumbled during parallel processing.
* Slightly optimized the Preprocess step to avoid going through parsers unnecessarily after a match.
* Fixed an issue with exception handling where the application would not pause on an unhandled exception. 