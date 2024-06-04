Note: This version adds an auto-update feature. If you're using the context menu Windows integration, you'll have to unregister the context menu one last time to install this update, and then re-register the context menu. The auto-updater will handle this smoothly moving forward.

* Added a self-update feature.
  * You may now choose "Update" when notified of a new version, which will perform an automatic update and restart the application with the previous command line arguments.
  * This also handles the annoying context menu registration process automatically.
* Moved the user settings file to AppData. If you need settings for a specific WitchyBND instance in its own folder, you can use an "appsettings.override.json" file.
* Moved the "DCX compression" context menu option to the main context menu, out of the "Process..." submenu, for the literal single person in the world that uses it frequently.
* Fixed an issue where command line options and context menu options would unintentionally save those options to the user settings file permanently.