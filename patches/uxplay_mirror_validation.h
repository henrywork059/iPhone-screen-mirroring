/*
 * Bound-checked validation for untrusted UxPlay mirroring packets.
 * Included in the pinned UxPlay source by our deterministic CI hardening step.
 * No memory access is performed until the entire requested range is validated.
 */
#ifndef UXPLAY_MIRROR_VALIDATION_H
#define UXPLAY_MIRROR_VALIDATION_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>

/* Reject unexpectedly large compressed frames before allocating memory.
 * 32 MiB exceeds normal compressed video frame sizes even at 4K.
 * This is deliberately an upper bound, not an assumed minimum frame size.
 */
#define UXPLAY_MIRROR_MAX_PAYLOAD (32u * 1024u * 1024u)
#define UXPLAY_MIRROR_MAX_PARAMETER_SET 1024u

static inline bool uxplay_mirror_valid_payload_size(int len)
{
    return len >= 0 && (uint64_t) len <= UXPLAY_MIRROR_MAX_PAYLOAD;
}

static inline bool uxplay_mirror_has_bytes(const unsigned char *data,
                                            size_t size, size_t offset, size_t needed)
{
    return data != NULL && offset <= size && needed <= size - offset;
}

/* Each NAL requires its four-byte big-endian length followed by at least
 * one payload byte. Do not dereference its first byte before this check. */
static inline bool uxplay_mirror_valid_nal(int frame_len, int offset, int nal_len)
{
    return frame_len > 0 && offset >= 0 && nal_len > 0 &&
           (size_t) offset <= (size_t) frame_len &&
           (size_t) frame_len - (size_t) offset >= 5u &&
           (size_t) nal_len <= (size_t) frame_len - (size_t) offset - 4u;
}

/* UxPlay HEVC codec records use a 4-byte signature followed by a 16-bit
 * big-endian byte count whose first byte overlaps signature byte 3.
 * This helper bounds-checks BOTH the header and the declared body.
 */
static inline bool uxplay_mirror_read_parameter_set(const unsigned char *data,
                                                    size_t size, size_t *offset,
                                                    const unsigned char expected[4],
                                                    const unsigned char **out,
                                                    size_t *out_len)
{
    if (!offset || !expected || !out || !out_len ||
        !uxplay_mirror_has_bytes(data, size, *offset, 5u))
        return false;

    const unsigned char *header = data + *offset;
    if (memcmp(header, expected, 4) != 0)
        return false;

    size_t length = ((size_t) header[3] << 8) | header[4];
    size_t payload_offset = *offset + 5u;
    if (length == 0 || length > UXPLAY_MIRROR_MAX_PARAMETER_SET ||
        !uxplay_mirror_has_bytes(data, size, payload_offset, length))
        return false;

    *out = data + payload_offset;
    *out_len = length;
    *offset = payload_offset + length;
    return true;
}

static inline bool uxplay_mirror_valid_h264_parameter_sets(size_t size,
                                                           int sps_size, int pps_size)
{
    /* 8-byte SPS prefix, one byte PPS count, 2-byte PPS length. */
    if (size < 11u || sps_size < 0 || pps_size < 0)
        return false;
    if ((size_t) sps_size > size - 11u)
        return false;
    return (size_t) pps_size <= size - (size_t) sps_size - 11u;
}

#endif
