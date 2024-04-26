* Fixed the "Reflection-based serialization has been disabled" error that has paralyzed parts of the program since switching to .NET 8.
* Fixed a logic error in the update check that caused the intended "check once every 6 hours" threshold to be ignored.
* Increased the update check threshold to every 24 hours.