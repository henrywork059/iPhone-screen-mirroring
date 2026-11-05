/* Real upstream mdnsd.c compressed-name parser, fuzzed without network sockets. */
#include <assert.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include "mdnsd/mdnsd.c"

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t size)
{
    if (size == 0 || size > 1500) return 0;
    char name[MDNSD_MAX_NAME];
    size_t offset = data[0] % size;
    int result = mdns_read_name(data, size, &offset, name, sizeof(name));
    if (result == 0) {
        assert(offset <= size);
        assert(memchr(name, '\0', sizeof(name)) != NULL);
    }
    return 0;
}
