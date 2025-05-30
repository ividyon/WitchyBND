[![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]

<p align="center">
  <img src="https://github.com/ividyon/WitchyBND/blob/main/public/logo.png?raw=true" />
</p>

# WitchyBND
WitchyBND is an unpacking/repacking/serializing software for common file formats used by FromSoftware's proprietary game engine, for games like Demon's Souls, Dark Souls 1-3, Bloodborne, Sekiro, Elden Ring and Armored Core VI. Witchy supports the formats DCX, FFXBND, BND3, BND4, BXF3, BXF4, FFXDLSE, FMG, GPARAM, LUAGNL, LUAINFO, TPF, Zero3, FXR3, MATBIN, MTD, PARAM, MQB, and ENTRYFILELIST.

A successor to **Yabber**, the FromSoftware file format unpacker and serializer by TKGP, featuring a comprehensive rewrite, added features and comfort, and bundled contributions by the community.

# Requirements
The game archives need to be unpacked with [UXM Selective Unpacker](https://github.com/Nordgaren/UXM-Selective-Unpack) to access the files that Witchy can work with.

WitchyBND should run out-of-the-box on Windows versions newer than Windows 8.

* For older versions, WitchyBND's context menu integration may require [.NET Framework 4.6](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net46).
* In case of unexpected issues, or if using Wine on Linux, [.NET Desktop Runtime 8.0](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe) may be necessary.

# Updating WitchyBND

To update WitchyBND, unpack the new version into the same folder as before, and overwrite all files.

The context menu unfortunately causes a bit of a fuss on updating. If Windows tells you that some of the files are in use, **you have to first unregister the context menu in the configuration screen**, then exit WitchyBND, to allow overwriting them.

After the update, you can re-register the context menu again.

[Read here how to configure the context menu integration.](https://github.com/ividyon/WitchyBND#right-click-context-menu-integration)

# How to use
Information on using Yabber (and therefore Witchy) is spread widely across the community. Visit the [Souls Modding Wiki](http://soulsmodding.wikidot.com/) or [?ServerName? Discord](http://discord.gg/servername) to get started.

## Right-click context menu integration
The most comfortable integration of WitchyBND is found in the context menu. Witchy does not come with an installer, so you need to briefly launch it and register the context menu with Windows yourself.

* Launch WitchyBND.exe in the Witchy folder.
* Navigate the configuration screen to the **"Configure Windows integration"** option, and confirm.
* Navigate to the **"Register WitchyBND context menu"** option, and confirm.

From now on, **WitchyBND** menu options should appear when you right-click files and folders in Explorer.

To remove the context menu options, simply use the **"Unregister WitchyBND context menu"** option. You may need to restart Explorer afterwards.

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

# Credits

This project is created and maintained by [ividyon](https://github.com/ividyon).
It is originally based on [Yabber](https://github.com/JKAnderson/Yabber) by TKGP.

* *TKGP* - SoulsFormats and Yabber
* *katalash* - GPARAM support, FsParam
* *Nordgaren* - Yabber+, ACVI additions, Oodle DLL location
* *DSMapStudio team* - Paramdex
* *Meowmaritus* - initial FXR serialization, SoulsAssetPipeline TAE class
* *NamelessHoodie* - initial FXR serialization
* *Avocado* - YabberAvocado additions
* *NatsuDragneelTheFireDragon* - MQB support (SoulsFormatsNEXT)
* *Vawser* - Paramdex, Smithbox additions
* *CCCode* - FXR research
* *ClayAmore* - ZSTD Decompression
* *The12thAvenger*: FLVER improvements (SoulsFormatsNEXT)
* *Shadowth117*: Console TPF handling (SoulsFormatsNEXT)
* All SoulsFormatsNEXT contributors
* All Paramdex contributors
* *ivi* - WitchyBND maintainer

Special thanks to Nordgaren, The12thAvenger, philiquaz, TKGP, thefifthmatt, Kirnifr, Rayan and many more for various assistance during development.

# License

This work is licensed under GPL v3. View the implications for your Witchy forks [here](https://www.tldrlegal.com/license/gnu-general-public-license-v3-gpl-3).

WitchyBND is built using the following licensed works:
* [SoulsFormats](https://github.com/JKAnderson/SoulsFormats/tree/er) by JKAnderson (see [License](licenses/LICENSE-SoulsFormats.md))
* [PromptPlus](https://github.com/FRACerqueira/PromptPlus), Copyright 2021 @ Fernando Cerqueira (see [License](licenses/LICENSE-PromptPlus.md))

# Changelog
## WitchyBND

### 2.15.0.0

* Added basic initial support for ELDEN RING NIGHTREIGN (thanks to Vawser and Nordgaren)
* Updated the handling of compression types to include more information. In short, this will make Witchy more robust against manipulation attempts on compression headers.
* Internally cleaned up various classes in significant ways. This may cause some regressions which will be quickly fixed once reported.
  * Several parser versions were raised due to this.

### 2.14.4.5

* Fixed assorted issues with the special ANIBND4 handling across different types of ANIBND in different games.
* Fixed an issue with the special FFXBND handling where empty FFXRESLISTs would try to be created for FFXBND archives that don't have them, such as Sekiro's or DS3's FFXBND.
* Updated SoulsFormatsNEXT.

### 2.14.4.4

* Updated to latest SoulsFormatsNEXT, fixing some issues with unpacking console textures. (thanks to Shadowth117)
* Updated to latest Paramdex from Smithbox. (thanks to Vawser & contributors)
* Fixed an MQB color repack bug. (thanks to Natsu)
* Changed some logic in determining AC6 param types to work with the updated Paramdex.

### 2.14.4.3

* Hotfix for supporting 1.16 PARAMs.
* Updated SoulsFormatsNEXT to fix a MQB bug.

### 2.14.4.2

* Updated SoulsFormatsNEXT.
* Updated Paramdex.

### 2.14.4.1

* Hotfix for supporting 1.15 PARAMs.

### 2.14.4.0

* Added support for Morpheme files in the ANIBND4 parser. They no longer need to be tracked in the _witchy-anibnd4.xml manifest file.
* Witchy will now try to remove the user settings file if it is corrupted.
* Fixed some issues with unpacking console TPF files (credit to Shadowth117).
* Fixed an issue where the Git backup settings had no effect.
* Fixed an issue where the error message for IOExceptions could be misleading.

### 2.14.3.1

* Updated SoulsFormatsNEXT.
* Updated Paramdex.

### 2.14.3.0

* Added customization of Witchy's backup method between the following choices:
  * Write the backup file unless one already exists. (Original behavior)
  * Overwrite the backup file every time. (New default behavior)
  * Create copies with unique names every time.
  * Do not back up.
* Added a toggle for whether Witchy should still perform backups for files that are inside a valid Git repository. This is on by default.
  * Setting this to off (no backups within Git repository) may impact performance, as the program checks for the presence of a Git repository every time before backing up.
* Added an opportunity to modify the arguments for a deferred tool when using an existing preset.
* Fixed an issue where FFXBND would not generate empty FFXRESLIST entries if the FFXBND folder does not have any FFXRESLISTs at all.
* Fixed an issue where FFXBND would crash if the FFXBND folder does not have FFXRESLISTs at all.
* Fixed an issue where PS3 TPFs written by Witchy would cause Demon's Souls to crash.
* Fixed an issue that caused BND3 files (and probably others) to fail to unpack.
* Fixed an issue with the warning when trying to repack PARAM files.

### 2.14.2.1

* Updated ZSTD compression to the default compression level (15).
* Fixed an issue with unpacking DCX files.

### 2.14.2.0

* Now supports ZSTD compression.

### 2.14.1.0

* Updated to latest Paramdex, including support for 1.13.2.
* When missing FFXRESLIST entries, the FFXBND parser will no longer print the full list of IDs, only the amount.
* Improved handling for completely empty FFXBNDs.
* Fixed an issue where Witchy would keep going through available parsers if an error occurred inside of one, even though the file matched a parser.

### 2.14.0.3

* Further bugfixes for the preprocess process for recursive repacking.

### 2.14.0.2

* Refined the Preprocess process further, eliminating duplicate work to speed it up.
  * Simplified the "is" check for recursive preprocess file type checks, where possible.
* If the selected location for "Process to..." is in a subfolder of the original location, relative paths will now be used.
* Fixed an issue where recursive repacking would not correctly preprocess the files.
* Fixed an issue where recursively unpacked files would still write "sourcePath" in their XML manifest when using the Location option.

### 2.14.0.1

* Fixed an issue where XML parsers would no longer extract to "filename.originalextension.xml", but to just "filename.xml".
* Fixed an issue where the Recursive option would generate a completely wrong path.

### 2.14.0.0

* Added specialized BND handling for ANIBND4. Unpacking and repacking these files will not require you to manage the file list for HKX and TAE.
  * This is well-tested for ELDEN RING, and lightly tested with BB. Testing in other games is welcome.
  * As a reminder, specialized binder handling can be turned off in the configuration, or via commandline.
* Fixed many issues with the Recursive and Preprocess systems. Recursive operations should now overall be more reliable.
* Fixed issues that occurred when using the Recursive and Location options together.
* Attempted a fix for an updater issue with WitchyBND.Shell.dll.

### 2.13.0.2

* Fixed an issue with the specialized FFXBND handler where the resulting DDS textures would not be headerized for console.

### 2.13.0.1

* Updated Paramdex to the latest from Smithbox.
* Updated SoulsFormatsNEXT to the latest (fixes for console TPF support)

### 2.13.0.0

* Updated WitchyBND to the latest version of SoulsFormatsNEXT, with several improvements courtesy of Shadowth117:
  * TPF platform support is now extended to:
    * PC: ✅ Unpack ✅ Repack
    * Xbox 360: ✅ Unpack ❌ Repack
    * PS3: ✅ Unpack ✅ Repack
    * PS4: ✅ Unpack ✅ Repack
    * PS5: ✅ Unpack ❌ Repack

    This should allow usage of WitchyBND for console modding in place of DesBNDBuild.
  * Malformed ELDEN RING cubemap DDS data will now be correctly parsed into readable DDS.
  * (Re-)Added a new texture type for texture arrays from Armored Core 6.
* Refactored "silent mode"; it will now avoid any output to PromptPlus, hopefully avoiding some issues.
* Updated Paramdex to the latest from Smithbox.
* Made some internal changes to error catching.

### 2.12.0.0

* Migrated WitchyBND to use [SoulsFormatsNEXT](https://github.com/soulsmods/SoulsFormatsNEXT).
  * Despite not being a noticable feature for end users, this will mark this update as a major version due to the internal restructure.
* Changed the "Flexible extraction" setting default to True. This will not apply to existing installs.
  * As a reminder, this setting foregoes some important format integrity checks in order to counteract intentional file corruption for the means of obfuscation.
* Updated Paramdex to the latest from Smithbox, including support for the latest regulation.


### 2.11.0.0

Note: This update may reset your personal settings.

* Added Deferred Format support for:
  * FLVER, with a preset for SoulsModelTool.
  * GFX, with a preset for JPEXS Free Flash Decompiler.
* Updated to latest Paramdex from Smithbox.
* Updated the MSBE serialization format.
* Updated the behavior of Deferred Formats.
  * Deferred Formats will no longer be processed as part of a Recursive process.
  * Deferred Formats can now perform repacking as well as unpacking.
  * Fixed an issue where the $path placeholder wasn't being populated in the arguments.
  * Fixed an issue where all presets would show regardless of intended format.
* Fixed issues that prevented the HKX and LUA deferred formats from working correctly.

### 2.10.0.4

* Removed the requirement for TAE events to be contained in the correct event bank.
  * This is kind of a janky workaround until a newer version of SoulsAssetPipeline is available.
* Updated to latest Paramdex from Smithbox.
* Optimized the PARAM parser.
  * Default values will now not be considered "above threshold" if there's less than 100 instances.
  * Added a boolean to fields which describes if the threshold was reached.
  * Optimized the logic with which default values are determined, to speed up the parser and hopefully fix issues with mismatching defaults between users.
* Fixed an issue where MSB serialization would have unnecessary indices.
* Changed the folder structure of the Assets folder.

### 2.10.0.3

* Improved MSBE serialization.
* Fixed an issue with the auto-updater where it would get stuck on the ZSTD library DLL.

### 2.10.0.2

* Set the "Flexible" feature to Off. This feature skipped important sanity checks which were abused by malicious actors like Garden of Eyes, but are once again important for working with potentially broken DLC files.
  * It can be re-enabled in options at your own risk.
* Fixed some issues with the MSB parser.
* Added some failsafes for text output crashing the actual process.
* Updated Paramdex to the latest from Smithbox.

### 2.10.0.1

* Fixed an issue where the 1.12 regulation.bin would not open due to a missing library file.
* Improved the error message when unable to open a regulation.bin file.

### 2.10.0.0

An initial release for basic ELDEN RING 1.12 (Shadow of the Erdtree) support.

* Added support for DCX ZSTD compression (used in ELDEN RING 1.12). Thanks to ClayAmore for the decompression code.
  * This allows unpacking the 1.12 regulation.bin file.
* Updated to the latest Paramdex from Smithbox.
  * This includes preliminary 1.12 PARAM support.
* Fixed an issue where unpacking a regulation.bin would needlessly ask for the PARAM version even though the archive provides it.
* Refined the error message that appears when a FFXBND is empty.

### 2.9.0.2

* Fixed an issue where FXRs wouldn't appear ingame. You need a ffxreslist file for each FXR after all, but it can be empty.
  * WitchyBND will now auto-generate an empty FFXRESLIST file for every FXR file in an FFX binder that doesn't have one.

### 2.9.0.1

* Replaced the error thrown when a FFXBND is missing reslist files, or has too many reslist files, with a notice.
  * As of now, they don't have any known effect, and FXRs work fine without reslists.
* Fixed an issue where the Offline Mode setting could not be toggled from the configuration menu.

### 2.9.0.0

Note: This version adds an auto-update feature. If you're using the context menu Windows integration, you'll have to unregister the context menu one last time to install this update, and then re-register the context menu. The auto-updater will handle this smoothly moving forward.

* Added a self-update feature.
  * You may now choose "Update" when notified of a new version, which will perform an automatic update and restart the application with the previous command line arguments.
  * This also handles the annoying context menu registration process automatically.
* Moved the user settings file to AppData. If you need settings for a specific WitchyBND instance in its own folder, you can use an "appsettings.override.json" file.
* Moved the "DCX compression" context menu option to the main context menu, out of the "Process..." submenu, for the literal single person in the world that uses it frequently.
* Fixed an issue where command line options and context menu options would unintentionally save those options to the user settings file permanently.

### 2.8.0.0

* Updated the FFXBND parser to version 2.8.0.0. Repack your FFXBNDs with the previous version of Witchy before updating, then unpack them anew.
  * Now supports older FFXBNDs like DS3 and BB.
  * No longer cares about the folder structure in the unpacked FFXBND folder whatsoever, so you can feel free to organize the files in your own way to keep an overview.
  * Confirmed that the TPFs changing is caused by SoulsFormats TPF padding being very relaxed. The data is fully intact, there are no issues with it.
* Fixed the WitchyBND format versioning system apparently never working outside of FXR.
* Fixed an issue with the FFXBND parser where it did not correctly check for orphaned FFXRESLIST files.
* Changed the message text for the DCX_KRAK warning.

### 2.7.1.0

* Improved the update notification.
  * You may now choose to skip a version to avoid being notified about it again.
  * You can now be directed to the release page directly, which will close Witchy.
  * The update time is no longer stored in a separate text file.
* Fixed an issue where the SFXBND parser would add one more backslash to paths than is necessary.
* Added a behind-the-scenes system for performing small updates upon the first run of any given WitchyBND version.
  * For now, this just deletes the "last-update.txt" file from before this version's update notification changes.
* Fixed some issues with publishing the program to Nexus.

### 2.7.0.0

* Added MSB serialization support for ELDEN RING.
  * This is exclusively for comparisons, and not intended to be used for serious editing in any way.
* Updated the PARAM parser to process "default values" differently. Default values will now always be added to the field tags even if they do not meet the threshold requirement. This is to avoid versioning issues with PARAM XMLs where the default value thresholds have changed, and formerly default value-fields no longer have a default value.
  * Updated the parser version to 2.7.1.0 due to potential breaking changes.
* Fixed an issue where drive paths would not be removed in BNDs which do not have a shared root folder (such as a hypothetical "different network drives" case).
* Fixed an issue where a DCX would be decompressed as a consolation prize if an error was thrown while unpacking what's inside the DCX.
* Fixed an issue that prevented AC4/ACFA regulations from opening.

### 2.6.2.1

* Fixed an issue that prevented Sekiro PARAM files from opening.

### 2.6.2.0

* Fixed the "Reflection-based serialization has been disabled" error that has paralyzed parts of the program since switching to .NET 8.
* Fixed an issue that caused the update check to run every single time, instead of max. once every 6 hours as intended.
* Increased the update check threshold to max. once every 24 hours.
* Added a description to the "PARAM Field Style" setting to explain what it does.
* Added previously unknown "IntColor" CustomData type to MQB. (Thanks to WarpZephyr)
* Further "encryption technology" countermeasures.
* Updated Paramdex.

### 2.6.1.1

* Fixed an issue with BND3 repacking incorrectly.
* Improved support for AC6 MQB files (thanks to WarpZephyr).

### 2.6.1.0

* Further work on "decrypting" some groundbreaking "encryption" "technology".
* Fixed a small visual issue where a decompressed DCX would not be counted as a processed item.

### 2.6.0.0

* Updated to .NET 8.
* Added "Flexible" option, set to True by default.
  * Loosens the strictness on some format value checks in order to correctly parse mod files with """encryption technology""" applied. Thanks to Vawser for the inspiration.
  * Set to False if structural integrity of files you create with your tools is important to you.
* Added "Unpack TAE to folder" option, set to True by default.
  * Enabling this option will unpack TAE files into a folder of XMLs, one for each animation.
  * Disabling it will serialize the TAE, with all constituent animations, into a single XML.
* Turned off HKX format support for the time being as it was being unpredictable and weird.
* Added configuration for PARAM field styles. This influences the output of field values in PARAM XMLs. Default is "Attribute".
* Made PARAM default value threshold configurable instead of toggling default values on/off.
  * Also set the default threshold from 60% to 80% of rows.
* Added -s "silent" parameter which attempts to suppress Witchy output. Useful when running Witchy in an environment that does not support its console output.
* Improved globbing behavior; you should no longer have to prepend `./` to relative paths to process them.
* Witchy will now attempt to throw an error when a deferred tool tries to ask for user input.
* Fixed an issue where the update checker would accidentally save arguments as configuration.
* Fix an issue with TAE and PARAM warnings when using Parallel processing.

### 2.5.0.0

* Added "deferred tool" support for HKX and LUA/HKS decompilation.
  * This includes formats that Witchy does not directly handle, but are handled by simple enough commandline tools that can be rolled into Witchy processing by being called externally. Witchy requires those tools to be installed for this to work.
  * To process deferred formats with Witchy, you need to download the according tool, unpack it somewhere, and configure the path to the tool in the Witchy configuration menu.
  * Once configured, Witchy will simply run the tool on supported files when they are processed via Witchy.
  * For starters, this includes support for HKX (via HKLib.CLI) and LUA/HKS (via DSLuaDecompiler).
  * Witchy lets you configure the arguments that are used by the tool. In the case of DSLuaDecompiler, it also comes with a pre-configured arguments preset.
* Added support for TAE serialization.
  * TAEs will be converted into a folder containing a _witchy-tae.xml file for TAE properties, and an animation XML for each animation in the TAE.
  * TAE deserialization comes with an obnoxious "are you sure?" warning like PARAM serialization, urging the user to use DSAnimStudio instead. Its purpose lies mainly in comparisons and version control.
* Restricted the version update check to occur only once every 6 hours, to avoid getting timed out by GitHub when running Witchy a lot.
* Improved game detection by scanning for executables and DSMS project.json content.
* Enabled parallel processing for PARAM rows.
* Updated Paramdex to the latest community values.
* Fixed an issue where "file not found" exceptions would be erroneously filed away as "in use by another process" errors.
* Fixed an issue where processing errors would erroneously display a "Could not find valid parser" error message.
* Fixed an issue where directories were not included in path globbing.
* Fixed a rare issue with file name case sensitivity.

### 2.4.1.0

* Changed the DCX behavior. If Witchy cannot find a valid parser for a DCX-compressed file, it will now decompress the DCX instead of doing nothing.
* Changed the method Witchy uses to restart the Explorer process, to hopefully fix some unreproducible issues.
* Added some minor error handling to the Paramdex unzipping process.

### 2.4.0.2

* Fixed a critical issue that corrupted the order of files in binders, reading to them being unreadable in some cases.

### 2.4.0.1

* Fixed an infinite loop when repacking FXR files.

### 2.4.0.0

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

### 2.3.0.2

* Fixed an issue where BND4 files could no longer open due to a mistake in the PARAMBND4 preprocess.
* Fixed an issue with manual input of regulation versions not correctly filtering fields by PARAMDEF regulation version.
* Restricted the DCX_KRAK warning to only appear if the archive contains more than 10 items.

### 2.3.0.1

* Fixed crashes with parallelized PARAM (de)serialization by introducing a Preprocess step to parsers.
  * Preprocess will run some operations over each path in the file list for the sake of not having to do them again per file.
  * This is currently in use by WPARAM and WPARAMBND4 parsers.

### 2.3.0.0

* Added parallel processing. When enabled, Witchy operations will be multi-threaded, which should speed them up.
  * This includes parallel-processing paths entered into Witchy, as well as operations performed on file lists in binders.
  * This is disabled by default and can be enabled in the configuration (by running the EXE standalone).
  * It may not be guaranteed that the order of files in the `<files>` section of the XML manifest is strictly followed when parallelization is enabled. Do not enable if this is a concern for you (but it shouldn't be in most cases).
* Added a notice when repacking using DCX_KRAK or DCX_KRAK_MAX compression, which are extremely slow. For development purposes, a compression like DCX_DFLT_11000_44_9_15 is recommended.
  * Did you know? A FFXBND repack which normally takes ~2-10 minutes with default compression, takes less than 1 second with no compression, and only 6 seconds with the above compression type.
* Updated FXR parser version.
  * Fully cleared up how States work. "Transitions" are now "StayConditions" and their output has been cleaned up greatly.
  * Replaced all remaining "Section" names by better approximations.
  * Renamed UnkReferences and UnkExternalValues to ReferenceList and ExternalValueList.
  * Changed the XML structure of Fields to just be `<Int Value="0">` and `<Float Value="0">` instead of the verbose `<Field xsi:Type="FFXFieldFloat" Value="0">`.
* Added a stopwatch which shows how long the Witchy operation lasted.
* Made Witchy less case-sensitive about the root tags of serialized XML files.
* Fixed an issue where MTD XMLs would not de-serialize back into MTD due to a certified :forestcat: moment.
* Fixed an issue where "Operation completed" would appear when closing the configuration UI via the Exit option.

### 2.2.0.1

* Unchanged release for the sake of running the fixed GitHub -> NexusMods pipeline.

### 2.2.0.0

* Added versioning for parsers.
  * In versioned parsers, WitchyBND will no longer accept files created by older versions, and will print an error message instead.
  * Parsers are unversioned by default and will receive versions upon significant, breaking format changes.
* Added versioning for the FXR parser.
  * Updated the FXR class the latest version from Rainbow Stone tool development.
  * This will cause breaking changes due to various improvements in the class structure reflecting in the XML.
* Remove notice about missing Paramdex names when unpacking PARAMs.
* Fixed an issue where the PARAM parser did not correctly filter the PARAMDEF for outdated, since-removed fields from older regulation versions.
* Fixed an regression from Yabber where PC save files could no longer be read after repacking due to incorrect IDs.
* Fixed an issue with the DBSUB parser where it would throw false positives on unrelated files.
* Updated Paramdex to the latest values from DSMapStudio.

### 2.1.1.1

* Updated Paramdex to the latest values from DSMapStudio.

### 2.1.1.0

* Added an informative warning to repacking PARAMBND, such as regulation.bin.
* Repacking PARAMBND4 will now perform a dry PARAM unpack attempt on all constituent PARAM files, to catch user errors attempting to pack outdated PARAMs into new regulation versions.
* Repacking PARAMBND4 will now fail if the regulation version exceeds the latest known regulation version in Paramdex.
* Paramdex detection is now stored per path during an application run, to allow unpacking PARAMs from different locations in the same program call.
* Fixed an issue with DS3 PARAM Paramdex detection where nested paths would fail to locate the regulation XML manifest.
* Added regulation version reading (and, if absent, prompting) to PARAM unpack.
* Updated Paramdex to the latest values from DSMapStudio.

### 2.1.0.1

* Fixed an issue where BXF binders (bdt/bhd) would not unpack or repack.

### 2.1.0.0

* Added specialized BND handling for the following binders: MATBINBND and MTDBND. Unpacking and repacking them will not require manipulating an XML file list.
  * As a reminder, specialized binder handling can be turned off in the configuration, or via commandline.
* Added support for the DBSUB format for Armored Core 4/For Answer subtitles, contributed by WarpZephyr.
* Added the option to add Witchy to the PATH environment variable in the configuration menu.
* Added a context menu item to process with standard BND handling.
* Added a context menu item to open the configuration menu.
* Improved performance by separating the decompression step from unpacking, as well as through other adjustments.
* Fixed an issue where the FFXBND handler would apply to DS3 FFXBNDs without handling them correctly.
  * DS3 FFXBNDs will use standard handling for the time being.
* Fixed an issue where user settings could save to the incorrect folder.
* Fixed an issue where MQB would create a folder instead of a file.
* Fixed an issue where trying to exit the configuration menu with Esc would instead select the current option.

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

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg