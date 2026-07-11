#include <cstddef>
#include <cstdint>
#include <limits>

#include "stdafx.h"
#include "compress.h"

#if defined(_WIN32)
#define WITCHY_OOZ_EXPORT extern "C" __declspec(dllexport)
#else
#define WITCHY_OOZ_EXPORT extern "C" __attribute__((visibility("default")))
#endif

WITCHY_OOZ_EXPORT int WitchyOoz_MaxCompressedSize(std::size_t input_size, std::size_t* result) {
  if (result == nullptr || input_size > static_cast<std::size_t>(std::numeric_limits<int>::max()) - 65536)
    return -1;
  *result = input_size + 65536;
  return 0;
}

WITCHY_OOZ_EXPORT int WitchyOoz_Compress(
    const std::uint8_t* input,
    std::size_t input_size,
    std::uint8_t* output,
    std::size_t output_capacity,
    int compressor,
    int level) {
  std::size_t required = 0;
  if (input == nullptr || output == nullptr ||
      WitchyOoz_MaxCompressedSize(input_size, &required) != 0 ||
      output_capacity < required)
    return -1;

  // Upstream optimal levels (5+) crash on validated large Elden Ring inputs.
  // Level 4 produces compatible streams and is the highest release-safe level.
  if (level > 4)
    level = 4;
  if (level < -4)
    return -1;

  return CompressBlock(
      compressor,
      const_cast<std::uint8_t*>(input),
      output,
      static_cast<int>(input_size),
      level,
      nullptr,
      nullptr,
      nullptr);
}
