#!/usr/bin/env bash
# CI ONLY: gather an unpublished candidate runtime and inspect dependencies.
# Does not start UxPlay. Does not create firewall exceptions or release artifacts.
set -euo pipefail
stage="${1:?stage directory required}"
[[ -s "$stage/bin/uxplay.exe" ]] || { echo "Missing staged UxPlay" >&2; exit 1; }
# Version lock for reviewed primary native media and crypto dependencies.
# Rolling MSYS2 repositories may change at any time: STOP rather than
# silently accept a new library until its advisories and license are reviewed.
while read -r package expected; do
  [[ -z "$package" || "$package" == \#* ]] && continue
  actual="$(pacman -Q "$package" | awk '{print $2}')"
  if [[ "$actual" != "$expected" ]]; then
    echo "SECURITY HOLD: $package changed from reviewed $expected to $actual" >&2
    exit 1
  fi
done <<'PINNED_MAIN_DEPENDENCIES'
mingw-w64-ucrt-x86_64-gstreamer 1.28.8-1
mingw-w64-ucrt-x86_64-gst-plugins-base 1.28.8-1
mingw-w64-ucrt-x86_64-gst-plugins-good 1.28.8-1
mingw-w64-ucrt-x86_64-gst-plugins-bad 1.28.8-3
mingw-w64-ucrt-x86_64-gst-libav 1.28.8-1
mingw-w64-ucrt-x86_64-ffmpeg 9.0.2-2
mingw-w64-ucrt-x86_64-openssl 3.6.5-1
mingw-w64-ucrt-x86_64-libplist 2.7.0-4
mingw-w64-ucrt-x86_64-json-glib 1.10.8-2
PINNED_MAIN_DEPENDENCIES
echo 'Reviewed primary MSYS2 dependency versions match exact pins.'
# GStreamer releases before 1.28.8 contain multiple October 2026
# security advisories; fail the build rather than bundle an older decoder.
gst_version="$(pkg-config --modversion gstreamer-1.0)"
oldest="$(printf '%s\n' '1.28.8' "$gst_version" | sort -V | head -n 1)"
[[ "$oldest" == '1.28.8' ]] || {
  echo "GStreamer $gst_version is below required security floor 1.28.8" >&2; exit 1;
}
echo "GStreamer minimum security version verified: $gst_version"
mkdir -p "$stage/lib/gstreamer-1.0" "$stage/libexec/gstreamer-1.0"
if [[ ! -d /ucrt64/lib/gstreamer-1.0 ]]; then echo "GStreamer plugin directory missing" >&2; exit 1; fi
# Only bundle plugin providers needed for mirroring H.264/H.265 and AAC/ALAC.
# Keep a conservative set for UxPlay's optional JPEG/playbin paths; do NOT
# include the complete global GStreamer registry or unrelated network plugins.
elements='appsrc queue capsfilter h264parse h265parse avdec_h264 avdec_h265 avdec_aac avdec_alac decodebin typefind videoconvert videoscale audioconvert audioresample volume level d3d11videosink wasapisink videoflip jpegdec imagefreeze textoverlay playbin playbin3'
for element in $elements; do
  details="$(gst-inspect-1.0 "$element")" || {
    echo "Missing required GStreamer element: $element" >&2; exit 1;
  }
  filename="$(printf '%s\n' "$details" | awk '$1 == "Filename" { print $2; exit }')"
  [[ -n "$filename" ]] || { echo "No provider filename for $element" >&2; exit 1; }
  plugin="$(printf '%s' "$filename" | tr '\\' '/' | awk -F/ '{print $NF}')"
  [[ "$plugin" == *.dll && -f "/ucrt64/lib/gstreamer-1.0/$plugin" ]] || {
    echo "Invalid or unavailable provider for $element: $filename" >&2; exit 1;
  }
  cp -p "/ucrt64/lib/gstreamer-1.0/$plugin" "$stage/lib/gstreamer-1.0/"
done
plugin_count="$(find "$stage/lib/gstreamer-1.0" -maxdepth 1 -type f -iname "*.dll" | wc -l)"
[[ "$plugin_count" -gt 0 && "$plugin_count" -le 35 ]] || {
  echo "GStreamer plugin subset unexpectedly large or empty: $plugin_count" >&2; exit 1;
}
echo "Allowlisted GStreamer plugins: $plugin_count"
# Inspect staged providers in isolation; no fallback to system plugins.
for element in $elements; do
  GST_PLUGIN_PATH_1_0="$stage/lib/gstreamer-1.0" \
  GST_PLUGIN_SYSTEM_PATH_1_0='' \
  GST_REGISTRY="$stage/gstreamer-test-registry.bin" \
    gst-inspect-1.0 "$element" >/dev/null || {
      echo "Staged plugin subset cannot provide $element" >&2; exit 1;
    }
done
rm -f "$stage/gstreamer-test-registry.bin"

if [[ -f /ucrt64/libexec/gstreamer-1.0/gst-plugin-scanner.exe ]]; then
  cp -p /ucrt64/libexec/gstreamer-1.0/gst-plugin-scanner.exe "$stage/libexec/gstreamer-1.0/"
fi
mapfile -d "" queue < <(find "$stage" -type f \( -iname "*.exe" -o -iname "*.dll" \) -print0)
declare -A visited=()
missing_file="$(mktemp)"
trap 'rm -f "$missing_file"' EXIT
index=0
while (( index < ${#queue[@]} )); do
  obj="${queue[$index]}"
  index=$((index + 1))
  [[ -n "${visited[$obj]+yes}" ]] && continue
  visited["$obj"]=1
  while IFS= read -r dep; do
    [[ -z "$dep" ]] && continue
    if [[ -f "/ucrt64/bin/$dep" ]]; then
      target="$stage/bin/$dep"
      if [[ ! -f "$target" ]]; then
        cp -p "/ucrt64/bin/$dep" "$target"
        queue+=("$target")
      fi
    elif [[ "$dep" == lib*.dll || "$dep" == lib*.DLL ]]; then
      echo "Unresolved non-system DLL import: $dep (referenced by $obj)" | tee -a "$missing_file" >&2
    fi
  done < <(objdump -p "$obj" 2>/dev/null | awk '/DLL Name:/ {print $3}')
done
if [[ -s "$missing_file" ]]; then echo "Native DLL dependencies unresolved" >&2; exit 1; fi
for required in libcrypto-3-x64.dll libgstreamer-1.0-0.dll libglib-2.0-0.dll libplist-2.0.dll; do
  [[ -f "$stage/bin/$required" ]] || { echo "Missing runtime DLL: $required" >&2; exit 1; }
done
echo "Native runtime direct/transitive DLL count: $(find "$stage/bin" -maxdepth 1 -iname "*.dll" | wc -l)"
echo "Native runtime staging size (not final package):"
du -sh "$stage"
cat > "$stage/NOT-APPROVED-FOR-INSTALLATION.txt" <<'EOF'
EXPERIMENTAL UxPlay receiver / Windows CI verification only.
DO NOT INSTALL, EXECUTE OR USE THIS RUNTIME FOR MIRRORING.
The complete application has not passed a security audit or real-device test.
This directory is deleted when its GitHub Actions job ends. No receiver release.
EOF
temp_manifest="$(mktemp)"
(cd "$stage" && find . -type f -print0 | sort -z | xargs -0 sha256sum) > "$temp_manifest"
mv "$temp_manifest" "$stage/SHA256SUMS.txt"
echo "PASS: candidate runtime dependencies collected and SHA-256 inventory recorded."
