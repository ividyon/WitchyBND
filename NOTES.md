* Added parallel processing. Witchy will now attempt to multi-thread its operations.
  * This can be disabled in the configuration.
* Added a notice when repacking using DCX_KRAK or DCX_KRAK_MAX compression, which are extremely slow. For development purposes, a compression like DCX_DFLT_11000_44_9_15 is recommended.
* Updated FXR parser version.
  * Fully cleared up how States work. "Transitions" are now "StayConditions" and their output has been cleaned up greatly.
  * Replaced all remaining "Section" names by better approximations.
  * Renamed UnkReferences and UnkExternalValues to ReferenceList and ExternalValueList.
  * Changed the XML structure of Fields to just be `<Int Value="0">` and `<Float Value="0">` instead of the verbose `<Field xsi:Type="FFXFieldFloat" Value="0">`.
* Added a stopwatch which shows how long the Witchy operation lasted.
* Fixed an issue where MTD XMLs would not de-serialize back into MTD due to a certified :forestcat: moment.
* Made Witchy less case-sensitive about the root tags of serialized XML files.
