* Restricted the version update check to occur only once every 6 hours, to avoid getting timed out by GitHub when running Witchy a lot.
* Fixed an issue where "file not found" exceptions would be erroneously filed away as "in use by another process" errors.
* Fixed an issue where processing errors would erroneously display a "Could not find valid parser" error message.
* Fixed a rare issue with file name case sensitivity.