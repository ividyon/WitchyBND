* Updated PARAM parsing to the newest version from DSMapStudio.
* Updated the Paramdex to the newest version from DSMapStudio and Vawser.
* Fixed an issue with PARAM serialization where ParamdefDataVersion and ParamdefFormatVersion would not be serialized from the PARAM, using the paramdef's values instead which could be outdated and incorrect.
* Fixed an issue with regulation.bin XML game detection if the already-unpacked XML from a previous version does not have the "game" attribute.
