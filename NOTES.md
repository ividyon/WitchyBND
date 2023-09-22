* Added specialized BND handling for the following binders: MATBINBND and MTDBND. Unpacking and repacking them will not require manipulating an XML file list.
  * As a reminder, specialized binder handling can be turned off in the configuration, or via commandline.
* Added support for the DBSUB format for Armored Core 4/For Answer subtitles, contributed by WarpZephyr.
* Added the option to add Witchy to the PATH environment variable in the configuration menu.
* Added a context menu item to process with standard BND handling.
* Added a context menu item to open the configuration menu.
* Improved performance by separating the decompression step from unpacking, as well as through other adjustments.
* Fixed an issue where the FFXBND handler would apply to DS3 FFXBNDs without handling them correctly.
  * DS3 FFXBNDs will use standard handling for the time being.
* Fixed an issue where user settings could save to the incorrect folder.
* Fixed an issue where MQB would create a folder instead of a file.
* Fixed an issue where trying to exit the configuration menu with Esc would instead select the current option.