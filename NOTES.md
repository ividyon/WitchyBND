* Separated ParamType from Paramdef Type in the XML serialization, to ensure that "tentative" ParamTypes do not get written to params.
* Fixed an issue that prevented params with duplicate field names from correctly serializing (such as AC6 BehaviorParam_PC).
  * Duplicate field names will now have the byte offset appended instead of the SortID.
* Fixed an issue where empty BNDs threw an exception when attempting to unpack them.