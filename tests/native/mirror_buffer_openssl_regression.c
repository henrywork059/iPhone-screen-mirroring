/*
 * Tests patched REAL upstream mirror_buffer.c and crypto.c with OpenSSL AES-CTR
 * and SHA-512, on synthetic frames. Not a full AirPlay or security audit.
 */
#include <assert.h>
#include <inttypes.h>
#include <openssl/evp.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "mirror_buffer.h"

static unsigned char plaintext_byte(unsigned p) {
    return (unsigned char)((p * 31U + (p >> 2) + 17U) & 255U);
}

static EVP_CIPHER_CTX *make_encrypter(uint64_t stream_id, const unsigned char key_audio[16]) {
    char text_key[64] = {0}, text_iv[64] = {0};
    unsigned char key_digest[64] = {0}, iv_digest[64] = {0};
    unsigned int keylen = 0, ivlen = 0;
    snprintf(text_key, sizeof(text_key), "AirPlayStreamKey%" PRIu64, stream_id);
    snprintf(text_iv, sizeof(text_iv), "AirPlayStreamIV%" PRIu64, stream_id);

    EVP_MD_CTX *digest = EVP_MD_CTX_new();
    assert(digest);
    assert(EVP_DigestInit_ex(digest, EVP_sha512(), NULL));
    assert(EVP_DigestUpdate(digest, text_key, strlen(text_key)));
    assert(EVP_DigestUpdate(digest, key_audio, 16));
    assert(EVP_DigestFinal_ex(digest, key_digest, &keylen));
    assert(keylen == 64);

    assert(EVP_DigestInit_ex(digest, EVP_sha512(), NULL));
    assert(EVP_DigestUpdate(digest, text_iv, strlen(text_iv)));
    assert(EVP_DigestUpdate(digest, key_audio, 16));
    assert(EVP_DigestFinal_ex(digest, iv_digest, &ivlen));
    assert(ivlen == 64);
    EVP_MD_CTX_free(digest);

    EVP_CIPHER_CTX *encrypt = EVP_CIPHER_CTX_new();
    assert(encrypt);
    assert(EVP_EncryptInit_ex(encrypt, EVP_aes_128_ctr(), NULL, key_digest, iv_digest));
    return encrypt;
}

static void test_sequence(const unsigned *lengths, size_t count) {
    unsigned char key_audio[16] = {0};
    for (unsigned i = 0; i < 16; ++i) key_audio[i] = (unsigned char)(i * 11 + 3);
    uint64_t stream = 991;
    mirror_buffer_t *receiver = mirror_buffer_init(NULL, key_audio);
    assert(receiver);
    mirror_buffer_init_aes(receiver, &stream);
    EVP_CIPHER_CTX *encrypt = make_encrypter(stream, key_audio);
    unsigned position = 0;

    for (size_t step = 0; step < count; ++step) {
        unsigned len = lengths[step];
        unsigned char *plain = malloc(len ? len : 1);
        unsigned char *cipher = malloc(len ? len : 1);
        unsigned char *output = malloc(len ? len : 1);
        assert(plain && cipher && output);
        for (unsigned j = 0; j < len; ++j) plain[j] = plaintext_byte(position + j);
        if (len > 0) {
            int written = 0;
            assert(EVP_EncryptUpdate(encrypt, cipher, &written, plain, (int)len));
            assert(written == (int)len);
        } else {
            output[0] = 0xA7;
        }

        mirror_buffer_decrypt(receiver, cipher, output, (int)len);
        if (len == 0) assert(output[0] == 0xA7);
        else if (memcmp(output, plain, len) != 0) {
            fprintf(stderr, "AES-CTR mismatch step=%zu len=%u total=%u\n", step, len, position);
            abort();
        }

        position += len;
        free(plain); free(cipher); free(output);
    }

    EVP_CIPHER_CTX_free(encrypt);
    mirror_buffer_destroy(receiver);
}

static void test_rekey(void) {
    unsigned char audio_key[16] = {0};
    mirror_buffer_t *receiver = mirror_buffer_init(NULL, audio_key);
    assert(receiver);
    uint64_t first = 42, second = 43;
    mirror_buffer_init_aes(receiver, &first);
    EVP_CIPHER_CTX *encrypt = make_encrypter(first, audio_key);
    unsigned char input = 0x32, cipher = 0, output = 0;
    int written = 0;
    assert(EVP_EncryptUpdate(encrypt, &cipher, &written, &input, 1));
    assert(written == 1);
    mirror_buffer_decrypt(receiver, &cipher, &output, 1);
    assert(output == input);
    EVP_CIPHER_CTX_free(encrypt);

    mirror_buffer_init_aes(receiver, &second);
    encrypt = make_encrypter(second, audio_key);
    input = 0xE1; cipher = 0; output = 0;
    assert(EVP_EncryptUpdate(encrypt, &cipher, &written, &input, 1));
    mirror_buffer_decrypt(receiver, &cipher, &output, 1);
    assert(output == input);
    EVP_CIPHER_CTX_free(encrypt);
    mirror_buffer_destroy(receiver);
}

int main(void) {
    const unsigned short_frames[] = {1, 1, 1, 2, 3, 0, 0, 4, 5, 8, 2, 3, 4, 31, 1, 7, 0, 13, 15, 17};
    const unsigned boundary[] = {12, 0, 0, 1, 1, 1, 1, 16, 16, 32, 33, 17, 64};
    const unsigned large[] = {16, 32, 18, 30, 1, 100, 9, 201};
    test_sequence(short_frames, sizeof(short_frames)/sizeof(short_frames[0]));
    test_sequence(boundary, sizeof(boundary)/sizeof(boundary[0]));
    test_sequence(large, sizeof(large)/sizeof(large[0]));
    for (unsigned n = 0; n <= 33; ++n) {
        const unsigned split[] = {1, n, 0, 1, 15, 1};
        test_sequence(split, sizeof(split)/sizeof(split[0]));
    }
    test_rekey();
    puts("PASS: real UxPlay OpenSSL AES-CTR/SHA-512 short-frame and re-key regression");
    return 0;
}
