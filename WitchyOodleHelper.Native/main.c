#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <errno.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef intptr_t (*oodle_decompress_fn)(
    const void*, intptr_t, void*, intptr_t, int, int, int, void*, intptr_t,
    void*, void*, void*, intptr_t, int);
typedef intptr_t (*oodle_compress_fn)(
    int, const void*, intptr_t, void*, int, const void*, const void*, void*,
    void*, intptr_t);
typedef intptr_t (*oodle_bound_fn)(intptr_t);
typedef const void* (*oodle_options_fn)(int, int);

typedef struct {
  int verbosity;
  int min_match_len;
  int seek_chunk_reset;
  int seek_chunk_len;
  int profile;
  int dictionary_size;
  int space_speed_tradeoff_bytes;
  int max_huffmans_per_chunk;
  int send_quantum_crcs;
  int max_local_dictionary_size;
  int make_long_range_matcher;
  int match_table_size_log2;
} oodle_compress_options;

static int read_file(const char* path, uint8_t** data, intptr_t* size) {
  FILE* file = fopen(path, "rb");
  if (file == NULL) {
    fprintf(stderr, "Could not open input file.\n");
    return 0;
  }
  if (_fseeki64(file, 0, SEEK_END) != 0) {
    fclose(file);
    return 0;
  }
  __int64 length = _ftelli64(file);
  if (length < 0 || (uint64_t)length > (uint64_t)INTPTR_MAX ||
      _fseeki64(file, 0, SEEK_SET) != 0) {
    fclose(file);
    return 0;
  }
  *size = (intptr_t)length;
  *data = (uint8_t*)malloc((size_t)(*size == 0 ? 1 : *size));
  if (*data == NULL || (*size > 0 && fread(*data, 1, (size_t)*size, file) != (size_t)*size)) {
    free(*data);
    fclose(file);
    return 0;
  }
  fclose(file);
  return 1;
}

static int write_file(const char* path, const uint8_t* data, intptr_t size) {
  FILE* file = fopen(path, "wb");
  if (file == NULL) {
    fprintf(stderr, "Could not open output file.\n");
    return 0;
  }
  int ok = size == 0 || fwrite(data, 1, (size_t)size, file) == (size_t)size;
  if (fclose(file) != 0)
    ok = 0;
  if (!ok)
    remove(path);
  return ok;
}

static int parse_intptr(const char* value, intptr_t* result) {
  char* end = NULL;
  errno = 0;
  __int64 parsed = _strtoi64(value, &end, 10);
  if (errno != 0 || end == value || *end != '\0' || parsed < 0 ||
      (uint64_t)parsed > (uint64_t)INTPTR_MAX)
    return 0;
  *result = (intptr_t)parsed;
  return 1;
}

static FARPROC require_export(HMODULE library, const char* name) {
  FARPROC result = GetProcAddress(library, name);
  if (result == NULL)
    fprintf(stderr, "Oodle DLL is missing required export %s.\n", name);
  return result;
}

int main(int argc, char** argv) {
  if (argc < 6) {
    fprintf(stderr,
        "Usage: WitchyOodleHelper <oodle-version> <oodle-dll> "
        "<decompress|compress> <input> <output> [size|compressor level]\n");
    return 2;
  }
  if (strcmp(argv[1], "6") != 0 && strcmp(argv[1], "8") != 0 && strcmp(argv[1], "9") != 0) {
    fprintf(stderr, "Oodle version must be 6, 8, or 9.\n");
    return 2;
  }

  HMODULE library = LoadLibraryA(argv[2]);
  if (library == NULL) {
    fprintf(stderr, "Could not load the configured Oodle DLL (error %lu).\n", GetLastError());
    return 1;
  }

  uint8_t* input = NULL;
  uint8_t* output = NULL;
  intptr_t input_size = 0;
  intptr_t output_capacity = 0;
  intptr_t written = -1;
  int exit_code = 1;
  if (!read_file(argv[4], &input, &input_size))
    goto cleanup;

  if (strcmp(argv[3], "decompress") == 0) {
    if (argc != 7 || !parse_intptr(argv[6], &output_capacity)) {
      fprintf(stderr, "Decompress requires a valid uncompressed size.\n");
      goto cleanup;
    }
    oodle_decompress_fn decompress =
        (oodle_decompress_fn)require_export(library, "OodleLZ_Decompress");
    if (decompress == NULL)
      goto cleanup;
    output = (uint8_t*)malloc((size_t)(output_capacity == 0 ? 1 : output_capacity));
    if (output == NULL)
      goto cleanup;
    written = decompress(input, input_size, output, output_capacity, 1, 0, 0,
        NULL, 0, NULL, NULL, NULL, 0, 3);
  } else if (strcmp(argv[3], "compress") == 0) {
    if (argc != 8) {
      fprintf(stderr, "Compress requires compressor and level values.\n");
      goto cleanup;
    }
    int compressor = atoi(argv[6]);
    int level = atoi(argv[7]);
    oodle_compress_fn compress =
        (oodle_compress_fn)require_export(library, "OodleLZ_Compress");
    oodle_bound_fn bound =
        (oodle_bound_fn)require_export(library, "OodleLZ_GetCompressedBufferSizeNeeded");
    oodle_options_fn get_options =
        (oodle_options_fn)require_export(library, "OodleLZ_CompressOptions_GetDefault");
    if (compress == NULL || bound == NULL || get_options == NULL)
      goto cleanup;
    output_capacity = bound(input_size);
    const oodle_compress_options* defaults =
        (const oodle_compress_options*)get_options(compressor, level);
    if (output_capacity <= 0 || defaults == NULL)
      goto cleanup;
    oodle_compress_options options = *defaults;
    options.seek_chunk_reset = 1;
    options.seek_chunk_len = 0x40000;
    output = (uint8_t*)malloc((size_t)output_capacity);
    if (output == NULL)
      goto cleanup;
    written = compress(compressor, input, input_size, output, level, &options,
        NULL, NULL, NULL, 0);
  } else {
    fprintf(stderr, "Unknown operation.\n");
    goto cleanup;
  }

  if (written < 0 || written > output_capacity) {
    fprintf(stderr, "Oodle returned an invalid output size.\n");
    goto cleanup;
  }
  if (!write_file(argv[5], output, written))
    goto cleanup;
  exit_code = 0;

cleanup:
  free(output);
  free(input);
  FreeLibrary(library);
  return exit_code;
}
