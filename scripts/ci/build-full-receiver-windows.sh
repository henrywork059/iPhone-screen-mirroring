#!/usr/bin/env bash
# BUILD-ONLY experimental feasibility check: NEVER start uxplay.exe or upload it.
# NOT a release, Windows safety approval, or authorized installation package.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
pinned="7aaad6222c2e136723e6e3d25c3be784260c577d"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
git -c core.autocrlf=false clone --quiet https://github.com/FDH2/UxPlay.git "$tmp/uxplay"
git -C "$tmp/uxplay" -c core.autocrlf=false checkout --quiet --detach "$pinned"
actual="$(git -C "$tmp/uxplay" rev-parse HEAD)"
[[ "$actual" == "$pinned" ]] || { echo "Unexpected UxPlay checkout $actual" >&2; exit 1; }
echo "Upstream UxPlay experimental pinned commit: $actual"
# Normalize Windows checkout EOLs in the CI-only source copy.
sed -i 's/\r$//' "$tmp/uxplay/lib/mirror_buffer.c"
sed 's/\r$//' "$root/patches/uxplay-experimental-mirror-short-frame.patch" > "$tmp/fix.patch"
git -C "$tmp/uxplay" apply --check "$tmp/fix.patch"
git -C "$tmp/uxplay" apply "$tmp/fix.patch"
echo 'Source patch applied. Running isolated C regression (no network listener).'
gcc -std=c11 -g -O2 -DWIN32 -D_WIN32 -Wall -Wextra -I "$tmp/uxplay/lib" \
  "$root/tests/native/mirror_buffer_regression.c" -o "$tmp/test_stub.exe" -lws2_32
"$tmp/test_stub.exe"
gcc -std=c11 -g -O2 -DWIN32 -D_WIN32 -ffunction-sections -fdata-sections -Wall -Wextra \
  -I "$tmp/uxplay/lib" "$tmp/uxplay/lib/mirror_buffer.c" "$tmp/uxplay/lib/crypto.c" "$tmp/uxplay/lib/utils.c" \
  "$root/tests/native/mirror_buffer_openssl_regression.c" \
  -Wl,--gc-sections -lcrypto -lws2_32 -lcrypt32 -lbcrypt -o "$tmp/test_openssl.exe"
"$tmp/test_openssl.exe"
echo "Configuring full receiver; build artifacts remain in the ephemeral CI runner."
cmake -S "$tmp/uxplay" -B "$tmp/build" -G Ninja \
  -DCMAKE_BUILD_TYPE=Release -DNO_MARCH_NATIVE=ON -DUSE_MDNS=ON
cmake --build "$tmp/build" --parallel 2
exe="$tmp/build/uxplay.exe"
[[ -s "$exe" ]] || { echo "Missing uxplay.exe output" >&2; exit 1; }
echo "Full receiver compiled. It will NOT be executed or uploaded."
echo "Build SHA-256 (informational; not a validated release hash):"
sha256sum "$exe"
echo "Imported runtime DLLs (for third-party dependency review):"
objdump -p "$exe" | grep -E "DLL Name:" || true
echo "Selected toolchain/dependency versions:"
pacman -Q mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-cmake \
  mingw-w64-ucrt-x86_64-libplist mingw-w64-ucrt-x86_64-gstreamer \
  mingw-w64-ucrt-x86_64-openssl
echo 'PASS: pinned patched experimental UxPlay source and full Windows executable build (NOT executed).'
