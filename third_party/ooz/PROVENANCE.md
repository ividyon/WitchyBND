# Witchy ooz source provenance

The files in this directory, except `witchy_ooz.cpp`, were copied from
`AnimeStudio.Oodle` at:

- Repository: `https://github.com/Escartem/AnimeStudio`
- Commit: `7cfb26b9c158f6150c887f9157de97a3fa7672e1`
- Retrieved: 2026-07-11

Only the source and SIMDe headers required to build the compression library are
vendored. The top-level AnimeStudio license is preserved as
`LICENSE-AnimeStudio-MIT.txt`. Individual source files retain their original
notices; notably, the Kraken decoder source includes GPLv3-or-later code.
WitchyBND itself is GPLv3, so that code is distributed under compatible terms.

`witchy_ooz.cpp` is a WitchyBND wrapper. It exports only bounded compression
functions. The upstream decoder is not exported through the Witchy API because
the validated revision decoded only one of six valid Elden Ring fixtures and
upstream warns that it is not fuzz-safe.

The wrapper clamps compression levels above 4 to level 4. The pinned upstream
compressor crashes on the validated 67,960,282-byte Elden Ring input at level
6, while level 4 produces a stream that official Oodle decodes byte-for-byte.
