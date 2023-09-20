<p align="center">
  <img src="https://github.com/ividyon/WitchyBND/blob/main/public/logo.png?raw=true" />
</p>

# WitchyBND v2.0.0.2
WitchyBND is an unpacking/repacking/serializing software for common file formats used by FromSoftware's proprietary game engine, for games like Demon's Souls, Dark Souls 1-3, Bloodborne, Sekiro, Elden Ring and Armored Core VI. Witchy supports the formats DCX, FFXBND, BND3, BND4, BXF3, BXF4, FFXDLSE, FMG, GPARAM, LUAGNL, LUAINFO, TPF, Zero3, FXR3, MATBIN, MTD, PARAM, and MQB.

A successor to **Yabber**, the FromSoftware file format unpacker and serializer by TKGP, featuring a comprehensive rewrite, added features and comfort, and bundled contributions by the community.

# Requirements
The game archives need to be unpacked with [UXM Selective Unpacker](https://github.com/Nordgaren/UXM-Selective-Unpack) to access the files that Witchy can work with.

WitchyBND should run out-of-the-box on Windows versions newer than Windows 8.

* For older versions, WitchyBND's context menu integration may require [.NET Framework 4.6](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net46).
* In case of unexpected issues, or if using Wine on Linux, [.NET Desktop Runtime 7.0](https://aka.ms/dotnet/7.0/windowsdesktop-runtime-win-x64.exe) may be necessary.

# Updating WitchyBND

To update WitchyBND, unpack the new version into the same folder as before, and overwrite all files.

Registering the WitchyBND context menu will put the files `WitchyBND.Shell.dll` and `Assets\context.png` in use, preventing them from being overwritten.

You have to first unregister the context menu in the configuration screen, and restart the Explorer process, then exit WitchyBND, to allow overwriting them.

# How to use
Information on using Yabber (and therefore Witchy) is spread widely across the community. Visit the [Souls Modding Wiki](http://soulsmodding.wikidot.com/) or [?ServerName? Discord](http://discord.gg/servername) to get started.

## Shell context menu
The most comfortable integration of WitchyBND is found in the context menu. Witchy does not come with an installer, so you need to briefly launch it and register the context menu with Windows yourself.

* Launch WitchyBND.exe in the Witchy folder.
* Navigate the configuration screen to the **"Configure shell integration"** option, and confirm.
* Navigate to the **"Register WitchyBND shell integration"** option, and confirm.

From now on, **WitchyBND** menu options should appear when you right-click files and folders in Explorer.

To remove the context menu options, simply use the **"Unregister WitchyBND shell integration"** option. You may need to restart Explorer afterwards.

## Basic workflow
* Unpack the game files using a tool like [UXM Selective Unpacker](https://www.nexusmods.com/eldenring/mods/1651).
* Find the files you'd like to extract.
* Use the right-click context menu "WitchyBND" option to process the selected files.
  * If processing over 15 files, you may need to use the "Send to..." menu. 
  * You can also drag the files onto the WitchyBND executable in the Witchy folder.
* If Witchy supports that file format, it will now be processed.
* To reverse the process, simply use the right-click context menu again.
  * You can also drag the unpacked folder (or converted file) or file back onto WitchyBND.exe again.

If you only want to remove the DCX compression from a DCX archive, use the **(DCX)** option in the context menu instead.

## Upgrading from Yabber
For all intents and purposes, Witchy should be treated as a new version of Yabber. It functions mostly the same and is used for all the same purposes. All the original workflows are preserved.

# Contributors
* *TKGP* - SoulsFormats and Yabber
* *katalash* - GPARAM support
* *Nordgaren* - Yabber+ additions, Armored Core VI additions
* *DSMapStudio team* - FsParam, Paramdex
* *Meowmaritus, NamelessHoodie* - initial FXR serialization
* *Avocado* - YabberAvocado additions
* *NatsuDragneelTheFireDragon* - MQB support
* *Vawser* - preliminary Armored Core VI paramdefs
* *ivi* - WitchyBND maintainer

Special thanks to Nordgaren, The12thAvenger, philiquaz, george_kingbore, katalash, TKGP and many more for various assistance during development.

# Changelog
## WitchyBND

### 2.0.1.0

Reminder: The context menu for Witchy needs to be unregistered before updating it to a new version, otherwise the Shell DLL file and context menu icon will report being "in use" and not allow replacing them.
Before applying this update, you have to manually kill the "explorer.exe" process in the Task Manager after unregistering the context menu. This will no longer be required for future releases.

* Fixed an issue where the Witchy icon would appear overly large in the context menu.
* Fixed an issue where context menu options would incorrectly appear for folders which do not contain a Witchy XML manifest.
* Added an Explorer restart to the context menu unregister process.
* Added documentation on updating Witchy to the README file.

### 2.0.0.2

A critical bugfix release.

#### PSA for v2

WitchyBND v2.0.0.0 introduced a **breaking change** with regulation files and PARAMs. Attempting to repack a PARAM XML with v2.0.0.0+ which was unpacked with an older Witchy version will cause the process to break.

To properly update:

* Repack any edited PARAMs and regulation files with your previous version of the tool.
* Update your WitchyBND to v2.0.0.0+.
* Unpack the regulation file, then PARAMs, with the new version. You are now updated.

#### Changes in v2.0.0.2

* Fixed a critical issue with binder file root paths that caused them to become corrupted due to a missing slash.
* Added some failsafes to make the PARAM breaking changes less catastrophic.
* Fixed an issue where "Pause on Error" was not toggled to TRUE by default.
* Fixed an issue where the AC6 TentativeParamTypes would be queried even if not interfacing with non-AC6 PARAMs.
* Fixed an issue where the wrong names appeared in the configuration overview for argument-only settings.

### 2.0.0.0

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

### 1.0.7.4

* Updated AC6 Paramdex to Vawser's latest version, fixing duplicate field name issues and more.
* Fixed an issue that would incorrectly wipe correct ParamTypes from serialized PARAMs.

### 1.0.7.3

* Separated ParamType from Paramdef Type in the XML serialization, to ensure that "tentative" ParamTypes do not get written to params.
* Fixed an issue that prevented params with duplicate field names from correctly serializing (such as AC6 BehaviorParam_PC).
  * Duplicate field names will now have the byte offset appended instead of the SortID.
* Fixed an issue where empty BNDs threw an exception when attempting to unpack them.

### 1.0.7.2

* Updated ACVI paramdefs to Vawser's latest version.
* Updated the AC6 PARAM reading solution for parity with the method and tentative ParamType names used by DSMapStudio.

### 1.0.7.1

* Updated ACVI paramdefs to Vawser's latest version.
* Applied a temporary solution to an issue that prevented some AC6 PARAMs from being read. Thanks to Vawser for the method.
* Fixed an issue with reading AC6 GIIV TPFs.

### 1.0.7.0

* Added support for decrypting/encrypting Armored Core VI regulation.bin. Thanks to Nordgaren for the encryption key.
* Added preliminary Paramdex support for Armored Core VI PARAM serialization. Thanks to Vawser for the preliminary paramdefs.
    * Please note that this will mostly be a bunch of Unknown fields; this is more of a tool for reverse engineering than anything else.

### 1.0.6.2

* ACTUALLY enabled use of Oodle 28 as promised in previous updates notes, just forgot to actually merge Nordgaren's change...
* See previous notes.

### 1.0.6.1

* Fixed incorrect versions in README and other texts

### 1.0.6.0

* Added detection and usage of Oodle 28 (from Armored Core VI) to SoulsFormats and Witchy. (By Nordgaren)
* Updated Fxr3 class to the newest findings from Rainbow Stone development. (This is **breaking** for the serialization, so convert all the XMLs you need to FXR before updating.)
* Fixed an oversight that could cause issues with error printing to the console.
* Small fixes behind the scenes to make working with the project easier for contributors.

### 1.0.5.0
* Added MQB support (by NatsuDragneelTheFireDragon)
* Added preliminary support for Armored Core VI DCX.
* Improved performance when serializing PARAM files.
* Updated Paramdex.

### 1.0.4.1
* Updated READMEs.

### 1.0.4.0
* New attempt at GPARAM support, using decompiled code from a random YabberAvocado zip. Appears to do a byte-perfect roundtrip unlike all other versions. Testing needed.
* Project cleanup for less warnings.
* Moved to NuGet versioning format (1.0.4.0 instead of 1.0.4).
* Properly display assembly info data.

### 1.0.3
* Fixed regulation.bin repacking as regulation.bin.dcx.

### 1.0.2
* Fixed embarassing bug preventing binders from being repacked.
* Added more graceful handling of failed TPF repacking for console TPFs.

### 1.0.1
* Moved to clean latest version of TKGP's SoulsFormats (ER branch)
* Improved handling of Paramdex name files
* Added support for console TPFs (by TK/DSMS team)
* Added support for MTD (by Avocado)
* Fixed small issue with AssetGeometryParam def being version 4 instead of 6

### 1.0.0
* Initial release.

## Yabber+
### 1.0.1 
* Minor fix to the Context menu registry (correct names)  
* Made it impossible to use multiple `..\` in a file path
* Updated version properly  

### 1.0  
* Added support for unpacking and repacking encrypted regulation BNDs  
* Updated oodle message to instruct users to get the file from Sekiro or Elden Ring  
* Fixed issue with files being written to incorrect folder, due to path traversal  
* There are some fixes and upgrades that were commited by TK before I ever looked at the project, that are in this build as well  

## Yabber
### 1.3.1
* DS2 .fltparams are now supported
* BXF4 repacking fixed
* Prompt for administrator access if necessary
* Breaking change: GPARAM format changed again; please repack any in-progress GPARAMs with the previous version, then unpack them again with this one

### 1.3
* Sekiro support
* Breaking change: GPARAM format has changed in a few ways; please repack any in-progress GPARAMs with the previous version, then unpack them again with this one

### 1.2.2
* Fix not being able to repack bnds with roots

### 1.2.1
* Fix LUAINFO not working on files with 2 or fewer goals
* Fix LUAGNL not working on some files
* Fix GPARAM not repacking files with Byte4 params
* Better support for weird BND/BXF formats without IDs or names

### 1.2
* GPARAM, LUAGNL, and LUAINFO are now supported
* Breaking change: compressed FMG is now supported; please repack any in-progress FMGs with the previous version, then unpack them again with this one

### 1.1.1
* Fix repacked FMGs getting double-spaced
* Fix decompressing DCXs that aren't named .dcx

### 1.1
* Add FMG support

### 1.0.2
* Fix repacking DX10 textures

### 1.0.1
* Fix bad BXF4 repacking