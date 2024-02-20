* Updated to .NET 8.
* Added "Flexible" option, set to True by default.
  * Loosens the strictness on some format value checks in order to correctly parse mod files with """encryption technology""" applied. Thanks to Vawser for the inspiration.
  * Set to False if structural integrity of files you create with your tools is important to you.
* Added "Unpack TAE to folder" option, set to True by default.
  * Enabling this option will unpack TAE files into a folder of XMLs, one for each animation.
  * Disabling it will serialize the TAE, with all constituent animations, into a single XML.
* Turned off HKX format support for the time being as it was being unpredictable and weird.
* Added configuration for PARAM field styles. This influences the output of field values in PARAM XMLs. Default is "Attribute".
* Made PARAM default value threshold configurable instead of toggling default values on/off.
  * Also set the default threshold from 60% to 80% of rows.
* Added -s "silent" parameter which attempts to suppress Witchy output. Useful when running Witchy in an environment that does not support its console output.
* Improved globbing behavior; you should no longer have to prepend `./` to relative paths to process them.
* Witchy will now attempt to throw an error when a deferred tool tries to ask for user input.
* Fixed an issue where the update checker would accidentally save arguments as configuration.
* Fix an issue with TAE and PARAM warnings when using Parallel processing.