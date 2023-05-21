## Note: This repo will not be updated. It only serves the purpose of archiving YabberAvocado, which is, as of November 13th, 2022, no longer maintained and unavailable to download from the original maintainer's repository.
Due to potential problems with the code, this project should only ever be used as a reference. Problems will not be addressed.

# Yabber
An unpacker/repacker for common Demon's Souls, Dark Souls 1-3, Bloodborne, and Sekiro file formats.\
Supports `.btab, .fxr, .msb, .blt, .matbin, .mtd, .bnd, .bhd/.bdt, .dcx, .fltparam, .fmg, .gparam, .luagnl, .luainfo, .tpf`.\
If you want to convert `.msb` files, you will need to create a file named `_er`, `_ds3`, etc. depending on the game the `.msb` is for (info is given when trying to convert).\
So far Demon's Souls `.msb` files can't be converted, sorry.

You can drag and drop a folder of files which are of 1 filetype to convert them all.\
Example: folder "A" has only files which end with `.fxr`. Drag and drop "A" onto Yabber to convert every `.fxr` file inside it.\
Now folder "A" will contain only files which end with `.fxr.xml` and will have a subfolder `BAK-fxr` where all the original `.fxr` were moved.\
If you drag and drop again folder "A" onto Yabber, you will be left again with `.fxr` files and two `BAK` folders - one for `.fxr` files and one for `.fxr.xml` files.

Does not support dvdbnds (the very large bhd/bdt pairs in the main game directory); use [UDSFM](https://www.nexusmods.com/darksouls/mods/1304) or [UXM](https://www.nexusmods.com/sekiro/mods/26) to unpack those first.\
Also does not support encrypted files (enc_regulation.bnd.dcx in DS2, Data0.bdt in DS3); you can edit these with [Yapped](https://www.nexusmods.com/darksouls3/mods/306) or unpack them with [BinderTool](https://github.com/Atvaark/BinderTool).\ 
Requires [.NET 6.0.0](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.8-windows-x64-installer) - Windows 10 users should already have this.

Please see the included readme for detailed instructions.

# Contributors
*katalash* - GPARAM support\
*TKGP* - Everything else\
*Rayan* - Testing
*Avocado* - Additional formats (among other features)

# Changelog
### 1.0.6 - Final Update
* Added Unk40 to gparams - fixed issues for ER

### 1.0.5
* Recursive un/repacking of folders (in other words subfolders get un/repacked too)

### 1.0.4
* Fixed Elden Ring gparams conversion
* Removed some unnecessary backups

### 1.0.3
* Preemptively backup files only when processing a folder for easier conversion back and forth.
* When processing file/files, backups are created only when the file/files to be created already exist.

### 1.0.2
* Bugfixes

### 1.0.1
* Drag and drop a folder to convert every file inside it
* Backup folders instead of files.
* FXR3 support including Elden Ring (xml)
* MATBIN and MTD converted to (xml) instead of (json)
* BTAB support (json)
* Slight refactor

### 1.0.0
* MSB files for the different games can be handled (ER, BB, Sekiro, DeS, DS3, DS2, DS1). (jsons)
* MATBIN support (json)
* MTD support (json)
* BLT support (json)
* added latest SoulsFormats from DSMapStudio to the repo
* updated Yabber accordingly to use .Net 6.0
* for previous changelog - look at the original Yabber from TKGP
