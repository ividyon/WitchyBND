* Updated the FFXBND parser to version 2.8.0.0. Repack your FFXBNDs with the previous version of Witchy before updating, then unpack them anew.
  * Now supports older FFXBNDs like DS3 and BB.
  * No longer cares about the folder structure in the unpacked FFXBND folder whatsoever, so you can feel free to organize the files in your own way to keep an overview.
  * Confirmed that the TPFs changing is caused by SoulsFormats TPF padding being very relaxed. The data is fully intact, there are no issues with it.
* Fixed the WitchyBND format versioning system apparently never working outside of FXR.
* Fixed an issue with the FFXBND parser where it did not correctly check for orphaned FFXRESLIST files.
* Changed the message text for the DCX_KRAK warning.