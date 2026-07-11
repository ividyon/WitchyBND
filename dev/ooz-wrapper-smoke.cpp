#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <vector>

extern "C" int WitchyOoz_MaxCompressedSize(std::size_t input_size, std::size_t* result);
extern "C" int WitchyOoz_Compress(
    const std::uint8_t* input,
    std::size_t input_size,
    std::uint8_t* output,
    std::size_t output_capacity,
    int compressor,
    int level);

static std::vector<std::uint8_t> ReadFile(const char* path) {
  std::FILE* file = std::fopen(path, "rb");
  if (file == nullptr)
    std::exit(2);
  std::fseek(file, 0, SEEK_END);
  long size = std::ftell(file);
  std::rewind(file);
  std::vector<std::uint8_t> data(static_cast<std::size_t>(size));
  if (size > 0 && std::fread(data.data(), 1, data.size(), file) != data.size())
    std::exit(3);
  std::fclose(file);
  return data;
}

int main(int argc, char** argv) {
  if (argc != 3 && argc != 4)
    return 1;
  std::vector<std::uint8_t> input = ReadFile(argv[1]);
  std::size_t capacity = 0;
  if (WitchyOoz_MaxCompressedSize(input.size(), &capacity) != 0)
    return 4;
  std::vector<std::uint8_t> output(capacity);
  int level = argc == 4 ? std::atoi(argv[3]) : 4;
  int written = WitchyOoz_Compress(
      input.data(), input.size(), output.data(), output.size(), 8, level);
  if (written <= 0 || static_cast<std::size_t>(written) > output.size())
    return 5;
  std::FILE* file = std::fopen(argv[2], "wb");
  if (file == nullptr)
    return 6;
  bool ok = std::fwrite(output.data(), 1, static_cast<std::size_t>(written), file) ==
      static_cast<std::size_t>(written);
  std::fclose(file);
  return ok ? 0 : 7;
}
