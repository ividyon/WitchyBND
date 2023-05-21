
--| Yabber 1.3.1
--| By TKGP
--| https://www.nexusmods.com/sekiro/mods/42
--| https://github.com/JKAnderson/Yabber

An unpacker/repacker for common Demon's Souls, Dark Souls 1-3, Bloodborne, and Sekiro file formats. Supports .bnd, .bhd/.bdt, .dcx, .fltparam, .fmg, .gparam, .luagnl, .luainfo, and .tpf.
In order to decompress Sekiro files you must copy oo2core_6_win64.dll from Sekiro into Yabber's lib folder.
Does not support dvdbnds (the very large bhd/bdt pairs in the main game directory); use UDSFM or UXM to unpack those first.
https://www.nexusmods.com/darksouls/mods/1304
https://www.nexusmods.com/sekiro/mods/26
Also does not support encrypted files (enc_regulation.bnd.dcx in DS2, Data0.bdt in DS3); you can edit these with Yapped or unpack them with BinderTool.
https://www.nexusmods.com/darksouls3/mods/306
https://github.com/Atvaark/BinderTool
Requires .NET 4.7.2 - Windows 10 users should already have this.
https://www.microsoft.com/net/download/thank-you/net472


--| Yabber.exe

This program is for unpacking and repacking supported formats. Drag and drop a file (bnd, bhd, fmg, gparam, luagnl, luainfo, or tpf) onto the exe to unpack it; drag and drop an unpacked folder to repack it. Multiple files or folders can be selected and dropped at a time.
DCX versions of supported formats can be dropped directly onto Yabber.exe without decompressing them separately; they will automatically be recompressed when repacking.
Edit the .xml file in the unpacked folder to add, remove or rename files before repacking.
Non-container files such as FMG or GPARAM are simply extracted to an xml file with the same name. Drop the .xml back onto Yabber to repack it.


--| Yabber.DCX.exe

This program is for decompressing and recompressing any DCX file. Drag and drop a DCX file onto the exe to decompress it; drag and drop the decompressed file to recompress it. Multiple files can be selected and dropped at a time.
You don't need to use this to decompress container formats before dropping them on Yabber.exe; this is only for compressed formats that aren't otherwise supported by Yabber.


--| Yabber.Context.exe

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

katalash - GPARAM support
TKGP - Everything else


--| Changelog

1.3.1
	DS2 .fltparams are now supported
	BXF4 repacking fixed
	Prompt for administrator access if necessary
	Breaking change: GPARAM format changed again; please repack any in-progress GPARAMs with the previous version, then unpack them again with this one

1.3
	Sekiro support
	Breaking change: GPARAM format has changed in a few ways; please repack any in-progress GPARAMs with the previous version, then unpack them again with this one

1.2.2
	Fix not being able to repack bnds with roots

1.2.1
	Fix LUAINFO not working on files with 2 or fewer goals
	Fix LUAGNL not working on some files
	Fix GPARAM not repacking files with Byte4 params
	Better support for weird BND/BXF formats without IDs or names

1.2
	GPARAM, LUAGNL, and LUAINFO are now supported
	Breaking change: compressed FMG is now supported; please repack any in-progress FMGs with the previous version, then unpack them again with this one

1.1.1
	Fix repacked FMGs getting double-spaced
	Fix decompressing DCXs that aren't named .dcx

1.1
	Add FMG support

1.0.2
	Fix repacking DX10 textures

1.0.1
	Fix bad BXF4 repacking
