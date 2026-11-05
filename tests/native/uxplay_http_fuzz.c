/*
 * Coverage-guided libFuzzer target for the ACTUAL UxPlay HTTP request parser
 * and reverse-HTTP event parser. No sockets are opened.
 */
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <assert.h>
#include "http_request.h"

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t size)
{
    if (size == 0 || size > 4096) return 0;
    http_request_t *rtsp = http_request_init();
    if (rtsp) {
        size_t first = size / 3;
        size_t second = size / 3;
        (void)http_request_add_data(rtsp, (const char *)data, first);
        (void)http_request_add_data(rtsp, (const char *)data + first, second);
        (void)http_request_add_data(rtsp, (const char *)data + first + second,
                                    size - first - second);
        if (http_request_is_complete(rtsp)) {
            (void)http_request_get_method(rtsp);
            (void)http_request_get_url(rtsp);
            (void)http_request_get_protocol(rtsp);
            int datalen = 0;
            (void)http_request_get_data(rtsp, &datalen);
        }
        http_request_destroy(rtsp);
    }

    http_request_t *event = http_request_init();
    if (event) {
        if (http_request_upgrade_data(event) == 0) {
            size_t chunk = size / 2;
            (void)http_request_add_event_data(event, (const char *)data, chunk);
            (void)http_request_add_event_data(event, (const char *)data + chunk,
                                               size - chunk);
            for (int n = 0; n < 4; ++n) {
                char *header = NULL, *body = NULL;
                size_t hlen = 0, blen = 0;
                if (!http_request_parse_event_data(event, &header, &hlen,
                                                   &body, &blen)) break;
                assert(header);
                assert(hlen <= 4096 && blen <= 4096);
                free(header);
                free(body);
            }
        }
        http_request_destroy(event);
    }
    return 0;
}
