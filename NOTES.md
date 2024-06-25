Important: These next releases continue to improve early support for Shadow of the Erdtree. However, you are advised to NOT do any modding work on existing projects while things are in flux.

There is a high likelihood of Witchy corrupting files or providing incomplete data, which in turn can irrevocably damage edited files.

These builds are primarily intended for datamining purposes.

* Removed the requirement for TAE events to be contained in the correct event bank.
  * This is kind of a janky workaround until a newer version of SoulsAssetPipeline is available.
* Updated to latest Paramdex from Smithbox.
* Optimized the PARAM parser.
  * Default values will now not be considered "above threshold" if there's less than 100 instances.
  * Added a boolean to fields which describes if the threshold was reached.
  * Optimized the logic with which default values are determined, to speed up the parser and hopefully fix issues with mismatching defaults between users.
* Fixed an issue where MSB serialization would have unnecessary indices.
* Changed the folder structure of the Assets folder.