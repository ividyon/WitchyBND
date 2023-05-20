--| WitchyBND
--| Maintained by ivi, Yabber created by TKGP
--| https://github.com/ividyon/WitchyBND

An unpacker/repacker for common Demon's Souls, Dark Souls 1-3, Bloodborne, Sekiro, Elden Ring file formats. Supports .bnd, .bhd/.bdt, .dcx, .fltparam, .fmg, .gparam, .luagnl, .luainfo, and .tpf.
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
Nordgaren - Yabber+ additions
DSMapStudio team - FSParam, Paramdex
NamelessHoodie - FXR serialization
Avocado - YabberAvocado additions
Pear - Tweaks to GPARAM
ivi - WitchyBND maintainer

--| Changelog

1.0.0
    Initial release.