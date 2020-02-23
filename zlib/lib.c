// https://github.com/richgel999/miniz/releases/tag/2.1.0
#include <stdint.h>
#include "miniz.c"

struct CompressionResult {
  int32_t result;
  uint32_t written;
};

int main() {}

#ifdef WIN32
__declspec(dllexport)
#else
#endif
struct CompressionResult zlib_compress_stream(uint8_t* destination, unsigned long destination_length, uint8_t* source, uint32_t source_length, int32_t compression_level)
{
  mz_ulong* size = &destination_length;
  int32_t result = compress2(destination, size, source, source_length, compression_level);

  struct CompressionResult compression_result;
  compression_result.written = *size;
  compression_result.result = result;
  return compression_result;
}