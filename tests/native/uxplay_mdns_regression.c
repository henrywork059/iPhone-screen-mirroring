/* ASan regression of real UxPlay mdns_read_name compressed DNS labels. */
#include <assert.h>
#include <stddef.h>
#include <string.h>
#include <stdio.h>
#include "mdnsd/mdnsd.c"
int main(void)
{
    unsigned char plain[] = {3,'f','o','o',5,'l','o','c','a','l',0};
    char name[MDNSD_MAX_NAME];
    size_t offset = 0;
    assert(mdns_read_name(plain, sizeof plain, &offset, name, sizeof name) == 0);
    assert(strcmp(name, "foo.local") == 0 && offset == sizeof plain);
    unsigned char ptr[] = {3,'f','o','o',0,0xc0,0};
    offset = 5;
    assert(mdns_read_name(ptr, sizeof ptr, &offset, name, sizeof name) == 0);
    assert(strcmp(name, "foo") == 0 && offset == sizeof ptr);
    unsigned char loop[] = {0xc0,0};
    offset = 0;
    assert(mdns_read_name(loop, sizeof loop, &offset, name, sizeof name) != 0);
    unsigned char trunc[] = {3,'x'};
    offset = 0;
    assert(mdns_read_name(trunc, sizeof trunc, &offset, name, sizeof name) != 0);
    puts("PASS: UxPlay mDNS compressed-name and malformed-packet regressions");
    return 0;
}
