* Added parallel processing. When enabled, Witchy operations will be multi-threaded.
  * This includes parallel-processing paths entered into Witchy, as well as operations on BND files.
  * This is disabled by default and can be enabled in the configuration.
* Added a notice when repacking using DCX_KRAK or DCX_KRAK_MAX compression, which are extremely slow. For development purposes, a compression like DCX_DFLT_11000_44_9_15 is recommended.
  * Did you know? A FFXBND repack which normally takes ~2-10 minutes with default compression, takes less than 1 second with no compression, and only 6 seconds with the above compression type.
* Updated FXR parser version.
  * Fully cleared up how States work. "Transitions" are now "StayConditions" and their output has been cleaned up greatly.
  * Replaced all remaining "Section" names by better approximations.
  * Renamed UnkReferences and UnkExternalValues to ReferenceList and ExternalValueList.
  * Changed the XML structure of Fields to just be `<Int Value="0">` and `<Float Value="0">` instead of the verbose `<Field xsi:Type="FFXFieldFloat" Value="0">`.
* Added a stopwatch which shows how long the Witchy operation lasted.
* Fixed an issue where MTD XMLs would not de-serialize back into MTD due to a certified :forestcat: moment.
* Made Witchy less case-sensitive about the root tags of serialized XML files.
