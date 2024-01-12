* Updated FXR parser version.
  * Fully cleared up how States work. "Transitions" are now "StayConditions" and their output has been cleaned up greatly.
  * Replaced all remaining "Section" names by better approximations.
  * Renamed UnkReferences and UnkExternalValues to ReferenceList and ExternalValueList.
  * Changed the XML structure of Fields to just be `<Int Value="0">` and `<Float Value="0">` instead of the verbose `<Field xsi:Type="FFXFieldFloat" Value="0">`.
* Made IsUnpacked detection for XML files case-insensitive when reading the root element name.