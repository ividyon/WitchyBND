* Added an informative warning to repacking PARAMBND, such as regulation.bin.
  * **DSMapStudio is the recommended tool for editing PARAMs**.
* Repacking PARAMBND4 will now perform a dry PARAM unpack attempt on all constituent PARAM files, to catch user errors attempting to pack outdated PARAMs into new regulation versions.
* Repacking PARAMBND4 will now fail if the regulation version exceeds the latest known regulation version in Paramdex.
* Paramdex detection is now stored per path during an application run, to allow unpacking PARAMs from different locations in the same program call.
* Fixed an issue with DS3 PARAM Paramdex detection where nested paths would fail to locate the regulation XML manifest.
* Added regulation version reading (and, if absent, prompting) to PARAM unpack.
* Fixed an issue where BXF binders (bdt/bhd) would not unpack or repack.
* Updated Paramdex to the latest values from DSMapStudio.
