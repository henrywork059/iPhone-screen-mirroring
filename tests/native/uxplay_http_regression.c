/*
 * ASan/UBSan regression of the REAL pinned UxPlay HTTP and reverse-HTTP code.
 * No network listener or iPhone connection is created.
 */
#include <assert.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "http_request.h"

static void check_event(const char *raw, size_t size,
                        const char *expected_header, const char *expected_body)
{
    http_request_t *request = http_request_init();
    assert(request);
    assert(http_request_upgrade_data(request) == 0);
    size_t half = size / 2;
    assert(http_request_add_event_data(request, raw, half) == 0);
    assert(http_request_add_event_data(request, raw + half, size - half) == 0);
    char *header = NULL, *body = NULL;
    size_t hlen = 0, blen = 0;
    assert(http_request_parse_event_data(request, &header, &hlen, &body, &blen));
    assert(header && strcmp(header, expected_header) == 0);
    assert(hlen == strlen(expected_header));
    if (expected_body) {
        assert(body && strcmp(body, expected_body) == 0);
        assert(blen == strlen(expected_body));
    } else {
        assert(body == NULL && blen == 0);
    }
    free(header); free(body);
    http_request_destroy(request);
}

static void check_reject(const char *input)
{
    http_request_t *request = http_request_init();
    assert(request);
    assert(http_request_upgrade_data(request) == 0);
    size_t len = strlen(input);
    assert(http_request_add_event_data(request, input, len) == 0);
    char *header = NULL, *body = NULL;
    size_t hl = 0, bl = 0;
    assert(!http_request_parse_event_data(request, &header, &hl, &body, &bl));
    assert(header == NULL && body == NULL);
    http_request_destroy(request);
}

int main(void)
{
    const char simple[] = "PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\nabc";
    const char prefix[] = "\001PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\nabc";
    const char no_body[] = "PTTH/1.0 200 OK\r\n\r\n";
    check_event(simple, strlen(simple), "PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\n", "abc");
    check_event(prefix, strlen(prefix), "PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\n", "abc");
    check_event(no_body, strlen(no_body), "PTTH/1.0 200 OK\r\n\r\n", NULL);
    check_reject("PTTH/1.0 200 OK\r\nContent-Length: 99999999\r\n\r\n");
    check_reject("PTTH/1.0 200 OK\r\nContent-Length: -1\r\n\r\n");
    check_reject("PTTH/1.0 200 OK\r\nContent-Length: 3xyz\r\n\r\nabc");
    check_reject("PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\nab");
    check_reject("PTTH/1.0 200 OK\r\nContent-Length: 4294967295\r\n\r\n");
    puts("PASS: upstream reverse-HTTP parser fragmented/leading-junk/invalid-length regressions");
    return 0;
}
