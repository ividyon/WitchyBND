<p align="center">
  <img src="https://github.com/ividyon/WitchyBND/blob/main/public/logo.png?raw=true" />
</p>

# WitchyBND
ivi's fork of Yabber, the binder unpacker and format serializer by TKGP, bundling contributions by various community members and adding some quality-of-life features.
WitchyBND continues where Yabber+ by Nordgaren left off.

This fork is focused on **ELDEN RING** development and is untested with other games in the series. TKGP's original Yabber is likely the best for those.

New additions include:
* Breaks down BND folder structure to avoid excessive nesting in unpacked folders
* File globbing pattern support in the commandline
* regulation.bin support (Decrypt->unpack and repack->encrypt)
* Slightly improved Oodle DLL detection
* PARAM support (using Paramdex and DefPatch. Compact, readable output)
* FXR support
* MATBIN support (by Avocado)
* MTD support (by Avocado)
* ELDEN RING GPARAM support (by Avocado)
* Console TPF unpacking (but not repacking)
* Minor fixes like ELDEN RING envmap TPF unpack

Planned (but TBD):
* Small UI for quality-of-life actions outside of unpacking/repacking (like duplicating AEGs, CHRs, PARTSBND...)
* Other formats from YabberAvocado (Let me know which ones are needed the most)
* Other formats from other Yabber forks (Let me know which are flying around the community)

## Yabber
An unpacker/repacker for common Demon's Souls, Dark Souls 1-3, Bloodborne, Sekiro, Elden Ring file formats. Supports .bnd, .bhd/.bdt, .dcx, .fltparam, .fmg, .gparam, .luagnl, .luainfo, and .tpf.
Does not support dvdbnds (the very large bhd/bdt pairs in the main game directory); use [UDSFM](https://www.nexusmods.com/darksouls/mods/1304) or [UXM](https://www.nexusmods.com/sekiro/mods/26) to unpack those first.  
Requires [.NET Desktop Runtime 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.5-windows-x64-installer).  

Please see the included readme for detailed instructions.  

# Contributors
* *TKGP* - Basically everything
* *katalash* - GPARAM support
* *Nordgaren* - Yabber+ additions
* *DSMapStudio team* - FSParam, Paramdex
* *NamelessHoodie* - FXR serialization
* *Avocado* - YabberAvocado additions
* *ivi* - WitchyBND maintainer

# Changelog
## WitchyBND

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