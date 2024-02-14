* TODO: Fix globs not starting with ./
* TODO: Fix "parser not found" AGAIN
* TODO: Fix GPARAM problem thing?
* Added "Unpack TAE to folder" option, set to True by default.
  * Enabling this option will unpack TAE files into a folder of XMLs, one for each animation.
  * Disabling it will serialize the TAE, with all constituent animations, into a single XML.
* Turned off HKX format support for the time being as it was being unpredictable and weird.
* Added configuration for PARAM field styles. This influences the output of field values in PARAM XMLs. Default is "Attribute".
* Added -s "silent" parameter which attempts to suppress Witchy output. Useful when running Witchy in an environment that does not support its console output.
* Witchy will now throw an error when a deferred tool tries to ask for user input.
* Fixed an issue where the update checker would accidentally save arguments as configuration.