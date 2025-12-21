* Added basic cross-platform capability with Linux support.
  * The oo2core binary needs to be provided by the user. See the README for more info.
  * Some features are unavailable.
* Updated PromptPlus to version 5, which should be more cross platform-compatible.
* Added support for the AIP (Auto Invade Point) format, props to Axi for the template.
* Removed the "DCX decompression only" configuration as it has only led to confusion so far. It can still be enabled via command line if needed.
* Fixed some crash issues with repacking PS4 TPFs. This updates the TPF parser version.
* Fixed a crash issue when handling FFXBND from different platforms than PC. This updates the FFXBND parser version.
* Fixed an issue with SoulsFormats TAE where certain event parameters would become corrupted when repacked.
* Fixed an issue where the ANIBND parser would ignore duplicately named files and proceed without throwing an error, causing jumbled output.
* Fixed an issue where archives would fail to unpack if they have no file extension.
* Added an option in the configuration menu to reset any version skips, so that the auto-updater may once again be ran.
