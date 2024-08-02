* Refined the Preprocess process further, eliminating duplicate work to speed it up.
  * Simplified the "is" check for recursive preprocess file type checks, where possible.
* If the selected location for "Process to..." is in a subfolder of the original location, relative paths will now be used.
* Fixed an issue where recursive repacking would not correctly preprocess the files.
* Fixed an issue where recursively unpacked files would still write "sourcePath" in their XML manifest when using the Location option.