/* Validation unit cases run under ASan/UBSan on untrusted byte lengths. */
#include <assert.h>
#include <stdint.h>
#include <stddef.h>
#include <stdio.h>
#include <limits.h>
#include "../../patches/uxplay_mirror_validation.h"

static void test_lengths(void)
{
    assert(uxplay_mirror_valid_payload_size(0));
    assert(uxplay_mirror_valid_payload_size((int) UXPLAY_MIRROR_MAX_PAYLOAD));
    assert(!uxplay_mirror_valid_payload_size(-1));
    assert(!uxplay_mirror_valid_payload_size(INT_MAX));

    unsigned char bytes[] = {0xa0, 0x00, 0x01, 0x00, 0x03, 10, 20, 30};
    const unsigned char sig[] = {0xa0, 0x00, 0x01, 0x00};
    const unsigned char *out = NULL;
    size_t len = 0;
    size_t offset = 0;
    assert(uxplay_mirror_read_parameter_set(bytes, sizeof bytes, &offset,
                                            sig, &out, &len));
    assert(len == 3 && offset == sizeof bytes && out[2] == 30);
    assert(!uxplay_mirror_read_parameter_set(bytes, sizeof bytes-1, &offset,
                                             sig, &out, &len));
    offset = 0;
    assert(!uxplay_mirror_read_parameter_set(bytes, 7, &offset, sig, &out, &len));
    offset = 0;
    assert(!uxplay_mirror_read_parameter_set(NULL, 8, &offset, sig, &out, &len));
    assert(!uxplay_mirror_read_parameter_set(bytes, sizeof bytes, NULL,
                                             sig, &out, &len));

    assert(!uxplay_mirror_valid_nal(0, 0, 1));
    assert(!uxplay_mirror_valid_nal(4, 0, 1));
    assert(uxplay_mirror_valid_nal(5, 0, 1));
    assert(!uxplay_mirror_valid_nal(5, 0, 2));
    assert(!uxplay_mirror_valid_nal(5, 1, 1));
    assert(!uxplay_mirror_valid_nal(5, 0, 0));
    assert(!uxplay_mirror_valid_nal(5, -1, 1));
    assert(!uxplay_mirror_valid_nal(5, 0, -1));

    assert(uxplay_mirror_valid_h264_parameter_sets(19, 4, 4));
    assert(!uxplay_mirror_valid_h264_parameter_sets(18, 4, 4));
    assert(!uxplay_mirror_valid_h264_parameter_sets(20, -1, 1));
    assert(!uxplay_mirror_valid_h264_parameter_sets(20, 1, -1));
    assert(!uxplay_mirror_valid_h264_parameter_sets(2, 1, 1));
}

/* Deterministic hostile length / offset and random codec-header corpus.
 * ASan/UBSan instrument the validation code, including tiny truncated buffers.
 */
static uint32_t state = 0xa843b25bu;
static uint32_t rng(void) {
    state ^= state << 13; state ^= state >> 17; state ^= state << 5;
    return state;
}
int main(void)
{
    test_lengths();
    const unsigned char sig[] = {0xa0, 0x00, 0x01, 0x00};
    for (int i = 0; i < 250000; ++i) {
        unsigned char data[128];
        for (size_t j = 0; j < sizeof data; ++j)
            data[j] = (unsigned char) rng();
        size_t size = rng() % (sizeof data + 1u);
        size_t off = rng() % (sizeof data + 2u);
        size_t original_off = off;
        const unsigned char *out = NULL;
        size_t n = 0;
        if (uxplay_mirror_read_parameter_set(data, size, &off, sig, &out, &n)) {
            assert(off > original_off && off <= size);
            assert(n > 0 && n <= UXPLAY_MIRROR_MAX_PARAMETER_SET);
            assert(out >= data && out+n <= data+size);
        } else {
            assert(off == original_off);
        }
        int len = (int)rng(), frame = (int)rng(), nal = (int)rng();
        bool valid = uxplay_mirror_valid_nal(frame,len,nal);
        if (valid)
            assert((size_t)frame-(size_t)len >= (size_t)nal+4);
        (void)uxplay_mirror_valid_h264_parameter_sets(size, len, nal);
    }
    puts("PASS: mirror network-frame bounds regression and hostile lengths");
    return 0;
}
