This is a major rewrite of WitchyBND to make it more extendable and customizable, as well as more comfortable to use for the end user. There are many changes to all aspects of the software, but overall it should still perform the same functions, just with more options than before, and with cleaner output.

**Please empty your old Witchy folder before installing this version**, due to the many file removals.

Due to being a large rewrite, bugs are expected. Please diligently report them.

* Major rewrite to the entire program.
* WitchyBND no longer requires external .NET runtimes to function for most computers.
  * Operating systems older than Windows 8 may need to install the .NET Framework 4.6 runtime to use the shell integration.
* Dramatically reduced the amount of files shipped with the program.
* Removed all executables except for WitchyBND.exe.
  * Registering the context menu is now done via the new configuration screen.
  * DCX decompression is now accessible via the context menu, commandline, or configuration screen.
* Added support for advanced commandline arguments, use the "--help" option for more information.
* Added a configuration screen that appears when launching WitchyBND without any arguments.
* Improved the Explorer context menu integration with new options in a dropdown menu.
  * Visit the configuration screen to enable the new shell integration.
  * Witchy's context menu is now separate from Yabber's.
* Added "Send to" shortcuts, for use when selecting more than 15 files (a Windows limitation).
* Implemented "recursive" binder handling, accessible via context menu, commandline, or configuration screen.
  * This will recursively process any files inside any unpacked binders right away.
* Implemented "Process to..." to decouple unpack location from source location.
  * In the context menu, opens a dialog to select the destination folder.
  * In the commandline, a path can directly be provided.
  * Processing files with this option adds their original path to the Witchy XML. When repacking, that path will be used.
* Compressed the Paramdex shipped with Witchy into a ZIP file.
  * The Paramdex will be unpacked automatically when needed for the first time, and replace any existing files.
* Added special handling of certain BNDs, beginning with FFXBND.
  * FFXBND no longer has a file list in its XML manifest. Files are distributed automatically when repacking.
  * TPFs in the FFXBND will now automatically unpack to DDS files.
  * This behavior is optional and can be disabled in the configuration screen, or commandline.
* Detection of file types now tries to avoid filename-based heuristics as much as possible.
* Added support for TextureArray TPFs introduced in Armored Core VI.
* Implemented DSMapStudio's TentativeParamType.CSV dictionary for handling Armored Core VI PARAM malfunctions.
* Updated Paramdex to the newest version from DSMapStudio.
* Fixed an issue where PARAMDEF versions would incorrectly overwrite PARAM versions in serialization.