* Added support for Oodle compression types which aren't used by Fromsoft, just because, and more robust detection of corrupting nonsense. Credits to colaaaaaa123.
* Witchy will now return a non-standard return code if there were any errors during its operation, allowing batch scripts to catch errors.
* Non-default config values are now highlighted in yellow during startup.
* Fixed an issue where BND3s during recursive processing would erroneously be detected as PARAMBND3.
* Fixed an issue where TAE enums could not properly be read during repack.