* Improved the update notification.
  * You may now choose to skip a version to avoid being notified about it again.
  * You can now be directed to the release page directly, which will close Witchy.
  * The update time is no longer stored in a separate text file.
* Fixed an issue where the SFXBND parser would add one more backslash to paths than is necessary.
* Added a behind-the-scenes system for performing small updates upon the first run of any given WitchyBND version.
  * For now, this just deletes the "last-update.txt" file from before this version's update notification changes.
* Fixed some issues with publishing the program to Nexus.