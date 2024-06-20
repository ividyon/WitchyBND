An initial release for basic ELDEN RING 1.12 (Shadow of the Erdtree) support.

* Added support for DCX ZSTD compression (used in ELDEN RING 1.12). Thanks to ClayAmore for the decompression code.
  * This allows unpacking the 1.12 regulation.bin file.
* Updated to the latest Paramdex from Smithbox.
  * This includes preliminary 1.12 PARAM support.
* Fixed an issue where unpacking a regulation.bin would needlessly ask for the PARAM version even though the archive provides it.
* Refined the error message that appears when a FFXBND is empty.