/*
 * Coverage-guided offline fuzz target for the exact bounded packet helpers
 * included in pinned UxPlay. This does not start any network listener.
 *
 * clang -fsanitize=fuzzer,address,undefined -O1 -g
 */
#include <stddef.h>
#include <stdint.h>
#include <limits.h>
#include "../../patches/uxplay_mirror_validation.h"

static int unpack_int(const uint8_t *data, size_t size, size_t i)
{
    if (i + 4 > size) return 0;
    uint32_t v = ((uint32_t)data[i]<<24) | ((uint32_t)data[i+1]<<16) |
                 ((uint32_t)data[i+2]<<8) | (uint32_t)data[i+3];
    return (int)(v & INT_MAX);
}

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t size)
{
    if (size > 4096) return 0;
    int payload_size = unpack_int(data,size,0);
    int offset = unpack_int(data,size,1);
    int nal_size = unpack_int(data,size,2);
    bool isValid = uxplay_mirror_valid_payload_size(payload_size);
    (void)isValid;
    (void)uxplay_mirror_valid_nal(payload_size, offset, nal_size);
    (void)uxplay_mirror_valid_h264_parameter_sets(size, offset, nal_size);
    if (size >= 1) {
        const unsigned char marker[] = {0xa0,0x00,0x01,0x00};
        size_t pos = (size_t)data[0] % (size+1u);
        const unsigned char *output = NULL;
        size_t n = 0;
        if (uxplay_mirror_read_parameter_set(data,size,&pos,marker,&output,&n)) {
            if (n == 0 || pos > size || output + n > data + size)
                __builtin_trap();
        }
        /* Include real-looking codec records to increase branch coverage. */
        pos = 0;
        (void)uxplay_mirror_read_parameter_set(data,size,&pos,marker,&output,&n);
    }
    return 0;
}
