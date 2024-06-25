Things are looking good, so I'm tentatively calling WitchyBND ready for Shadow of the Erdtree modding. Of course, issues may still pop up, but so far none have been detected.

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
