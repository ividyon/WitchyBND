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