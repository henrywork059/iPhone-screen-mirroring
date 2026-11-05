#!/usr/bin/env bash
# Functional-only Windows compilation of pinned, patched UxPlay decryption C.
# No listener, network service, distribution, or installer is built.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
commit="df67c212a433cf6dda3676dd40c097900d24e645"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
git clone --quiet --depth 1 --branch v1.73.7 https://github.com/FDH2/UxPlay.git "$tmp/uxplay"
actual="$(git -C "$tmp/uxplay" rev-parse HEAD)"
[[ "$actual" == "$commit" ]] || { echo "Unexpected UxPlay revision: $actual" >&2; exit 1; }
git -C "$tmp/uxplay" apply --check "$root/patches/uxplay-mirror-short-frame.patch"
git -C "$tmp/uxplay" apply "$root/patches/uxplay-mirror-short-frame.patch"
gcc -std=c11 -g -O2 -DWIN32 -D_WIN32 -Wall -Wextra \
  -I "$tmp/uxplay/lib" "$root/tests/native/mirror_buffer_regression.c" \
  -o "$tmp/mirror_stub_regression.exe" -lws2_32
"$tmp/mirror_stub_regression.exe"
gcc -std=c11 -g -O2 -DWIN32 -D_WIN32 -ffunction-sections -fdata-sections -Wall -Wextra \
  -I "$tmp/uxplay/lib" "$tmp/uxplay/lib/mirror_buffer.c" "$tmp/uxplay/lib/crypto.c" \
  "$root/tests/native/mirror_buffer_openssl_regression.c" \
  -Wl,--gc-sections -lcrypto -lws2_32 -lcrypt32 -lbcrypt \
  -o "$tmp/mirror_openssl_regression.exe"
"$tmp/mirror_openssl_regression.exe"
