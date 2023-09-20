Reminder: The context menu for Witchy needs to be unregistered before updating it to a new version, otherwise the Shell DLL file and context menu icon will report being "in use" and not allow replacing them.
Before applying this update, you have to manually kill the "explorer.exe" process in the Task Manager after unregistering the context menu. This will no longer be required for future releases.

* Fixed an issue where the Witchy icon would appear overly large in the context menu.
* Fixed an issue where context menu options would incorrectly appear for folders which do not contain a Witchy XML manifest.
* Added an Explorer restart to the context menu unregister process.
* Added documentation on updating Witchy to the README file.