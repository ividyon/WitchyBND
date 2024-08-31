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

