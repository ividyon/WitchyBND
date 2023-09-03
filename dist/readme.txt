--| WitchyBND
--| 1.0.7.3
--| Maintained by ivi, Yabber created by TKGP
--| https://github.com/ividyon/WitchyBND

An unpacker/repacker for common Demon's Souls, Dark Souls 1-3, Bloodborne, Sekiro, Elden Ring, Armored Core VI file formats. Supports .bnd, .bhd/.bdt, .dcx, .fltparam, .fmg, .gparam, .luagnl, .luainfo, and .tpf.
Does not support dvdbnds (the very large bhd/bdt pairs in the main game directory); use UDSFM or UXM unpack those first.
Requires .NET Desktop Runtime 7.0.

--| WitchyBND.exe

This program is for unpacking and repacking supported formats. Drag and drop a file (bnd, bhd, fmg, gparam, luagnl, luainfo, tpf, fxr and more...) onto the exe to unpack it; drag and drop an unpacked folder to repack it. Multiple files or folders can be selected and dropped at a time.
DCX versions of supported formats can be dropped directly onto WitchyBND.exe without decompressing them separately; they will automatically be recompressed when repacking.
Edit the .xml file in the unpacked folder to add, remove or rename files before repacking.
Non-container files such as FMG or GPARAM are simply extracted to an xml file with the same name. Drop the .xml back onto WitchyBND to repack it.

--| WitchyBND.DCX.exe

This program is for decompressing and recompressing any DCX file. Drag and drop a DCX file onto the exe to decompress it; drag and drop the decompressed file to recompress it. Multiple files can be selected and dropped at a time.
You don't need to use this to decompress container formats before dropping them on WitchyBND.exe; this is only for compressed formats that aren't otherwise supported by WitchyBND.


--| WitchyBND.Context.exe

This program registers the other two so that they can be run by right-clicking on a file or folder. Run it to choose whether to register or unregister them.
The other two programs are assumed to be in the same folder. If you move them, just run it again from the new location.


--| Formats

BND3
Extension: .*bnd
A generic file container used before DS2. DS1 is fully supported; DeS is mostly supported.

BND4
Extension: .*bnd
A generic file container used since DS2.

BXF3
Extensions: .*bhd, .*bdt
A generic file container split into a header and data file, used before DS2. Only drag-and-drop the .bhd to unpack it; the .bdt is assumed to be in the same directory.

BXF4
Extensions: .*bhd, .*bdt
A generic file container split into a header and data file, used since DS2. Only drag-and-drop the .bhd to unpack it; the .bdt is assumed to be in the same directory.

DCX
Extension: .dcx
A single compressed file, used in all games.

FMG
Extension: .fmg
A collection of text strings with an associated ID number, used in all games. %null% is a special keyword indicating an ID that is present but has no text.

GPARAM
Extension: .fltparam, .gparam
A graphical configuration format used since DS2.

LUAGNL/LUAINFO
Extension: .luagnl/.luainfo
Lua scripting support files used in all games except DS2.

TPF
Extension: .tpf
A DDS texture container, used in all games. Console versions are not supported.


--| Contributors

TKGP - Basically everything
katalash - GPARAM support
Nordgaren - Yabber+ additions, Armored Core VI additions
DSMapStudio team - FSParam, Paramdex
Meowmaritus, NamelessHoodie - initial FXR serialization
Avocado - YabberAvocado additions
NatsuDragneelTheFireDragon - MQB support
Vawser - preliminary Armored Core VI paramdefs
ivi - WitchyBND maintainer

--| Changelog

1.0.7.3
    Separated ParamType from Paramdef Type in the XML serialization, to ensure that "tentative" ParamTypes do not get written to params.
    Fixed an issue that prevented params with duplicate field names from correctly serializing (such as AC6 BehaviorParam_PC).
        Duplicate field names will now have the byte offset appended instead of the SortID.
    Fixed an issue where empty BNDs threw an exception when attempting to unpack them.

1.0.7.2
    Updated ACVI paramdefs to Vawser's latest version.
    Updated the AC6 PARAM reading solution for parity with the method and tentative ParamType names used by DSMapStudio.

1.0.7.1
    Updated ACVI paramdefs to Vawser's latest version.
    Applied a temporary solution to an issue that prevented some AC6 PARAMs from being read. Thanks to Vawser for the method.
    Fixed an issue with reading AC6 GIIV TPFs.

1.0.7.0
    Added support for decrypting/encrypting Armored Core VI regulation.bin. Thanks to Nordgaren for the encryption key.
    Added preliminary Paramdex support for Armored Core VI PARAM serialization. Thanks to Vawser for the preliminary paramdefs.
        Please note that this will mostly be a bunch of Unknown fields; this is more of a tool for reverse engineering than anything else.

1.0.6.2
    ACTUALLY enabled use of Oodle 28 as promised in previous updates notes, just forgot to actually merge Nordgaren's change...
    See previous notes.

1.0.6.1
    Fixed incorrect versions in README and other texts

1.0.6.0
    Added detection and usage of Oodle 28 (from Armored Core VI) to SoulsFormats and Witchy. (By Nordgaren)
    Updated Fxr3 class to the newest findings from Rainbow Stone development. (This is **breaking** for the serialization, so convert all the XMLs you need to FXR before updating.)
    Fixed an oversight that could cause issues with error printing to the console.
    Small fixes behind the scenes to make working with the project easier for contributors.

1.0.5.0
    Added MQB support (by NatsuDragneelTheFireDragon)
    Added preliminary support for Armored Core VI DCX.
    Improved performance when serializing PARAM files.
    Updated Paramdex.

1.0.4.1
    Updated READMEs.

1.0.4.0
    New attempt at GPARAM support, using decompiled code from a random YabberAvocado zip. Appears to do a byte-perfect roundtrip unlike all other versions. Testing needed.
    Project cleanup for less warnings.
    Moved to NuGet versioning format (1.0.4.0 instead of 1.0.4).
    Properly display assembly info data.

1.0.3
    Fixed regulation.bin repacking as regulation.bin.dcx.

1.0.2
    Fixed embarassing bug preventing binders from being repacked.
    Added more graceful handling of failed TPF repacking for console TPFs.

1.0.1
    Moved to clean latest version of TKGP's SoulsFormats (ER branch)
    Improved handling of Paramdex name files
    Added support for console TPFs (by TK/DSMS team)
    Added support for MTD (by Avocado)
    Fixed small issue with AssetGeometryParam def being version 4 instead of 6

1.0.0
    Initial release.