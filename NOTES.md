### PSA for v2

WitchyBND v2.0.0.0 introduced a **breaking change** with regulation files and PARAMs. Attempting to repack a PARAM XML with v2.0.0.0+ which was unpacked with an older Witchy version will cause the process to break.

To properly update:

* Repack any edited PARAMs and regulation files with your previous version of the tool.
* Update your WitchyBND to v2.0.0.0+.
* Unpack the regulation file, then PARAMs, with the new version. You are now updated.

### Changes in v2.0.0.1

* Added some failsafes to make the PARAM breaking changes less catastrophic.
* Fixed an oversight where "Pause on Error" was not toggled to TRUE by default.
* Fixed an oversight where the AC6 TentativeParamTypes would be queried even if not interfacing with non-AC6 PARAMs.
