#!/usr/bin/env bash
# Offline sanitizer test of patched pinned upstream C. Does NOT launch an AirPlay listener.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
commit="df67c212a433cf6dda3676dd40c097900d24e645"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
git clone --quiet --depth 1 --branch v1.73.7 https://github.com/FDH2/UxPlay.git "$tmp/uxplay"
actual="$(git -C "$tmp/uxplay" rev-parse HEAD)"
if [[ "$actual" != "$commit" ]]; then
  echo "Unexpected upstream SHA: $actual" >&2
  exit 1
fi
git -C "$tmp/uxplay" apply --check "$root/patches/uxplay-mirror-short-frame.patch"
git -C "$tmp/uxplay" apply "$root/patches/uxplay-mirror-short-frame.patch"
cc -std=c11 -g -O1 -fno-omit-frame-pointer -fsanitize=address,undefined \
   -Wall -Wextra -I "$tmp/uxplay/lib" \
   "$root/tests/native/mirror_buffer_regression.c" \
   -o "$tmp/mirror_buffer_regression" -pthread
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 \
UBSAN_OPTIONS=halt_on_error=1 \
  "$tmp/mirror_buffer_regression"

# Exercise the real OpenSSL AES-CTR and SHA-512 code, too.
cc -std=c11 -g -O1 -fno-omit-frame-pointer -ffunction-sections -fdata-sections \
   -fsanitize=address,undefined -Wall -Wextra -I "$tmp/uxplay/lib" \
   "$tmp/uxplay/lib/mirror_buffer.c" "$tmp/uxplay/lib/crypto.c" \
   "$root/tests/native/mirror_buffer_openssl_regression.c" \
   -Wl,--gc-sections -lcrypto -lm -pthread -o "$tmp/mirror_buffer_openssl_regression"
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 \
UBSAN_OPTIONS=halt_on_error=1 \
  "$tmp/mirror_buffer_openssl_regression"

# Exercise the exact packet/codec-size helper compiled into the experimental
# UxPlay receiver. This is offline deterministic fuzzing, not full protocol fuzz.
cc -std=c11 -g -O1 -fno-omit-frame-pointer -fsanitize=address,undefined \
   -Wall -Wextra -Werror "$root/tests/native/mirror_validation_regression.c" \
   -o "$tmp/mirror_validation_regression"
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 \
UBSAN_OPTIONS=halt_on_error=1 \
  "$tmp/mirror_validation_regression"


# Offline sanitizer/fuzzer coverage of the actual pinned UxPlay HTTP parser.
git clone --quiet https://github.com/FDH2/UxPlay.git "$tmp/uxplay-http"
git -C "$tmp/uxplay-http" checkout --quiet --detach 7aaad6222c2e136723e6e3d25c3be784260c577d
[[ "$(git -C "$tmp/uxplay-http" rev-parse HEAD)" == 7aaad6222c2e136723e6e3d25c3be784260c577d ]] || exit 1
git -C "$tmp/uxplay-http" apply --check "$root/patches/uxplay-http-request-bounds.patch"
git -C "$tmp/uxplay-http" apply "$root/patches/uxplay-http-request-bounds.patch"
common=(-std=c11 -O1 -g -D_GNU_SOURCE -fno-omit-frame-pointer -fsanitize=address,undefined -I "$tmp/uxplay-http/lib")
sources=("$tmp/uxplay-http/lib/http_request.c" "$tmp/uxplay-http/lib/llhttp/api.c"
         "$tmp/uxplay-http/lib/llhttp/http.c" "$tmp/uxplay-http/lib/llhttp/llhttp.c")
clang -std=c11 -O1 -g -D_GNU_SOURCE -fno-omit-frame-pointer -fsanitize=address,undefined \
  -I "$tmp/uxplay-http/lib" "$tmp/uxplay-http/lib/http_request.c" \
  "$tmp/uxplay-http/lib/llhttp/api.c" "$tmp/uxplay-http/lib/llhttp/http.c" \
  "$tmp/uxplay-http/lib/llhttp/llhttp.c" "$root/tests/native/uxplay_http_regression.c" \
  -o "$tmp/http_regression"
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 UBSAN_OPTIONS=halt_on_error=1 "$tmp/http_regression"
clang -std=c11 -O1 -g -D_GNU_SOURCE -fno-omit-frame-pointer -fsanitize=fuzzer,address,undefined \
  -I "$tmp/uxplay-http/lib" "$tmp/uxplay-http/lib/http_request.c" \
  "$tmp/uxplay-http/lib/llhttp/api.c" "$tmp/uxplay-http/lib/llhttp/http.c" \
  "$tmp/uxplay-http/lib/llhttp/llhttp.c" "$root/tests/native/uxplay_http_fuzz.c" \
  -o "$tmp/http_fuzzer"
mkdir -p "$tmp/http_corpus"
printf 'GET /info HTTP/1.1\r\nHost: local\r\n\r\n' > "$tmp/http_corpus/info"
printf '\001PTTH/1.0 200 OK\r\nContent-Length: 3\r\n\r\nabc' > "$tmp/http_corpus/ptth"
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 UBSAN_OPTIONS=halt_on_error=1 \
  "$tmp/http_fuzzer" -runs=50000 -max_len=4096 -timeout=4 "$tmp/http_corpus"


# Real native UxPlay mDNS compressed-label parser; no sockets or mDNS threads.
# Link unused mdnsd service code out with function-section garbage collection.
for target in mdns_regression mdns_fuzzer; do
  if [[ "$target" == mdns_regression ]]; then
    testsource="$root/tests/native/uxplay_mdns_regression.c"
    extra=""
  else
    testsource="$root/tests/native/uxplay_mdns_fuzz.c"
    extra="-fsanitize=fuzzer"
  fi
  clang -std=c11 -D_GNU_SOURCE -g -O1 -fno-omit-frame-pointer \
    -ffunction-sections -fdata-sections -fsanitize=address,undefined \
    -I "$tmp/uxplay-http/lib" $extra \
    "$testsource" -Wl,--gc-sections -pthread -o "$tmp/$target"
done
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 UBSAN_OPTIONS=halt_on_error=1 "$tmp/mdns_regression"
mkdir -p "$tmp/mdns_corpus"
printf '\003foo\005local\000' > "$tmp/mdns_corpus/valid"
printf '\300\000' > "$tmp/mdns_corpus/loop"
ASAN_OPTIONS=detect_leaks=1:halt_on_error=1 UBSAN_OPTIONS=halt_on_error=1 \
  "$tmp/mdns_fuzzer" -runs=40000 -max_len=1500 -timeout=4 "$tmp/mdns_corpus"
