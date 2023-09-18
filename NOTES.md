This is a major rewrite of WitchyBND to make it more extendable and customizable, as well as more comfortable to use for the end user. There are many changes to all aspects of the software, but overall it should still perform the same functions, just with more options than before, and with cleaner output.

Due to being a large rewrite, bugs are expected. Please diligently report them.

* Major rewrite to the entire program.
* WitchyBND now supports advanced commandline arguments, use the "--help" option for more information.
* Added a configuration screen that appears when launching WitchyBND without any arguments.
  * Many of the commandline arguments can be made default behavior in the configuration screen.
* Implemented (optional) shell integration for WitchyBND, an advancement of the previous context menu integration.
  * Visit the configuration screen to enable the shell integration.
  * Context menu entries can now be conditional based on what you selected.
  * "Send to" shortcuts have been added for use when selecting more than 15 files (a Windows limitation).
  * Disable the shell integration and restart explorer.exe before moving or removing the WitchyBND folder.
* Removed all executables except for WitchyBND.exe, to avoid user confusion.
  * Registering the context menu is now done via the new configuration screen.
  * Decompressing DCX is now done via the context menu, commandline arguments, or by setting it as default behavior in the configuration screen.
* Implemented a "recursive" mode, via commandline arguments or the configuration screen.
  * Enabling this mode will automatically process all files that are unpacked from binders, recursively.
* Files can now be processed to a different folder than their location, via "Process To..."
  * This is accessible via the context menu and the commandline.
* Added new custom handling of certain BNDs, beginning with FFXBND.
  * FFXBND will now eschew using a file list in the XML file, and instead automatically assign correct file IDs to the unpacked folder contents.
  * TPFs in the FFXBND will now automatically unpack to DDS files.
  * The aim is to make working with FFXBND less of a bureaucratic pain, by removing some of the hoops to jump through.
  * This behavior can be disabled in the configuration screen.
* Detection of file types now tries to avoid filename-based heuristics as much as possible.
* Added support for TextureArray TPFs introduced in Armored Core VI.
* Updated Paramdex to the newest version from DSMapStudio.