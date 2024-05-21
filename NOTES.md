* Updated the PARAM parser to process "default values" differently. Default values will now always be added to the field tags even if they do not meet the threshold requirement. This is to avoid versioning issues with PARAM XMLs where the default value thresholds have changed, and formerly default value-fields no longer have a default value.
  * Updated the parser version to 2.7.1.0 due to potential breaking changes.
* Fixed an issue where drive paths would not be removed in BNDs which do not have a shared root folder (such as a hypothetical "different network drives" case).
* Fixed an issue where a DCX would be decompressed as a consolation prize if an error was thrown while unpacking what's inside the DCX.
