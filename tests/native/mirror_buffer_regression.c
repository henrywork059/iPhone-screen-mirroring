/*
 * Exercises real upstream lib/mirror_buffer.c compiled under ASan and UBSan.
 * Crypto primitives are DETERMINISTIC TEST DOUBLES. This is NOT cryptographic,
 * AirPlay-protocol, Windows-runtime, or comprehensive security validation.
 */
#include <assert.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "mirror_buffer.c"   /* actual UxPlay C file from pinned upstream checkout */

struct aes_ctx_s { unsigned position; };
struct sha_ctx_s { unsigned unused; };

static unsigned char stream_byte(unsigned pos) {
    return (unsigned char)(((pos * 73U) ^ (pos >> 2) ^ 0xA5U) & 255U);
}
static unsigned char plain_byte(unsigned pos) {
    return (unsigned char)((pos * 47U + 0x19U) & 255U);
}

aes_ctx_t *aes_ctr_init(const uint8_t *key, const uint8_t *iv) {
    (void) key; (void) iv;
    return calloc(1, sizeof(aes_ctx_t));
}
void aes_ctr_destroy(aes_ctx_t *ctx) { free(ctx); }
void aes_ctr_start_fresh_block(aes_ctx_t *ctx) {
    if (ctx->position % 16U)
        ctx->position += 16U - (ctx->position % 16U);
}
void aes_ctr_decrypt(aes_ctx_t *ctx, const uint8_t *in, uint8_t *out, int len) {
    assert(len >= 0);
    for (int i = 0; i < len; ++i)
        out[i] = in[i] ^ stream_byte(ctx->position + (unsigned)i);
    ctx->position += (unsigned)len;
}

sha_ctx_t *sha_init(void) { return calloc(1, sizeof(sha_ctx_t)); }
void sha_update(sha_ctx_t *ctx, const uint8_t *in, int len) {
    (void)ctx; (void)in; (void)len;
}
void sha_final(sha_ctx_t *ctx, uint8_t *out, unsigned int *len) {
    (void) ctx;
    if (out) memset(out, 0x42, 32);
    if (len) *len = 32;
}
void sha_reset(sha_ctx_t *ctx) { (void)ctx; }
void sha_destroy(sha_ctx_t *ctx) { free(ctx); }

static mirror_buffer_t *new_receiver(void) {
    const unsigned char key[RAOP_AESKEY_LEN] = {0};
    mirror_buffer_t *receiver = mirror_buffer_init(NULL, key);
    assert(receiver);
    uint64_t stream = 42;
    mirror_buffer_init_aes(receiver, &stream);
    assert(receiver->aes_ctx);
    return receiver;
}

static void test_sequence(const unsigned *lengths, size_t count) {
    mirror_buffer_t *receiver = new_receiver();
    unsigned position = 0;
    for (size_t step = 0; step < count; ++step) {
        unsigned len = lengths[step];
        unsigned char *input = malloc(len ? len : 1);
        unsigned char *output = malloc(len ? len : 1);
        assert(input && output);
        for (unsigned j = 0; j < len; ++j)
            input[j] = plain_byte(position + j) ^ stream_byte(position + j);
        if (!len) output[0] = 0xE7;
        mirror_buffer_decrypt(receiver, input, output, (int)len);
        if (len == 0) {
            assert(output[0] == 0xE7);
        } else {
            for (unsigned j = 0; j < len; ++j) {
                if (output[j] != plain_byte(position + j)) {
                    fprintf(stderr, "mismatch at fragment %zu len %u global offset %u byte %u\n",
                            step, len, position, j);
                    abort();
                }
            }
        }
        free(input);
        free(output);
        position += len;
    }
    mirror_buffer_destroy(receiver);
}

static void test_rekey(void) {
    mirror_buffer_t *receiver = new_receiver();
    unsigned char cipher[1] = {plain_byte(0) ^ stream_byte(0)};
    unsigned char output[1] = {0};
    mirror_buffer_decrypt(receiver, cipher, output, 1);
    assert(output[0] == plain_byte(0));
    assert(receiver->nextDecryptCount == 15);
    const uint64_t next_id = 89;
    mirror_buffer_init_aes(receiver, &next_id);
    assert(receiver->nextDecryptCount == 0);
    assert(receiver->aes_ctx->position == 0);
    unsigned char second[1] = {plain_byte(0) ^ stream_byte(0)};
    output[0] = 0;
    mirror_buffer_decrypt(receiver, second, output, 1);
    assert(output[0] == plain_byte(0));
    mirror_buffer_destroy(receiver);
}

int main(void) {
    const unsigned short_frames[] = {1, 1, 1, 2, 3, 0, 0, 4, 5, 8, 2, 3, 4, 31, 1, 7, 0, 13, 15, 17};
    const unsigned boundary[] = {12, 0, 0, 1, 1, 1, 1, 16, 16, 32, 33, 17, 64};
    const unsigned ordinary[] = {16, 32, 18, 30, 1, 100, 9, 201};
    const unsigned empty_first[] = {0, 0, 0, 1, 0, 0, 2, 0, 0, 3, 6, 0, 0, 7};
    test_sequence(short_frames, sizeof(short_frames)/sizeof(short_frames[0]));
    test_sequence(boundary, sizeof(boundary)/sizeof(boundary[0]));
    test_sequence(ordinary, sizeof(ordinary)/sizeof(ordinary[0]));
    test_sequence(empty_first, sizeof(empty_first)/sizeof(empty_first[0]));
    test_rekey();

    for (unsigned n = 0; n <= 33; ++n) {
        const unsigned split[] = {1, n, 0, 1, 15, 1};
        test_sequence(split, sizeof(split)/sizeof(split[0]));
    }

    mirror_buffer_t *receiver = new_receiver();
    mirror_buffer_decrypt(receiver, NULL, NULL, -1);
    mirror_buffer_decrypt(receiver, NULL, NULL, 0);
    mirror_buffer_destroy(receiver);
    puts("PASS: upstream C short-frame, empty-frame, boundary, and rekey tests");
    return 0;
}
