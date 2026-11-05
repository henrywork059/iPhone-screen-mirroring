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
