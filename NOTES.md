* Added "deferred tool" support for HKX and LUA/HKS decompilation.
  * This includes formats that Witchy does not directly handle, but are handled by simple enough commandline tools that can be rolled into Witchy processing by being called externally. Witchy requires those tools to be installed for this to work.
  * To process deferred formats with Witchy, you need to download the according tool, unpack it somewhere, and configure the path to the tool in the Witchy configuration menu.
  * Once configured, Witchy will simply run the tool on supported files when they are processed via Witchy.
  * For starters, this includes support for HKX (via HKLib.CLI) and LUA/HKS (via DSLuaDecompiler).
  * Witchy lets you configure the arguments that are used by the tool. In the case of DSLuaDecompiler, it also comes with a pre-configured arguments preset.
* Added support for TAE serialization.
  * TAEs will be converted into a folder containing a _witchy-tae.xml file for TAE properties, and an animation XML for each animation in the TAE.
  * TAE deserialization comes with an obnoxious "are you sure?" warning like PARAM serialization, urging the user to use DSAnimStudio instead. Its purpose lies mainly in comparisons and version control.
* Restricted the version update check to occur only once every 6 hours, to avoid getting timed out by GitHub when running Witchy a lot.
* Improved game detection by scanning for executables and DSMS project.json content.
* Enabled parallel processing for PARAM rows.
* Fixed an issue where "file not found" exceptions would be erroneously filed away as "in use by another process" errors.
* Fixed an issue where processing errors would erroneously display a "Could not find valid parser" error message.
* Fixed an issue where directories were not included in path globbing.
* Fixed a rare issue with file name case sensitivity.