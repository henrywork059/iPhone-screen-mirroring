#!/usr/bin/env bash
# CI ONLY: gather an unpublished candidate runtime and inspect dependencies.
# Does not start UxPlay. Does not create firewall exceptions or release artifacts.
set -euo pipefail
stage="${1:?stage directory required}"
[[ -s "$stage/bin/uxplay.exe" ]] || { echo "Missing staged UxPlay" >&2; exit 1; }
mkdir -p "$stage/lib/gstreamer-1.0" "$stage/libexec/gstreamer-1.0"
if [[ ! -d /ucrt64/lib/gstreamer-1.0 ]]; then echo "GStreamer plugin directory missing" >&2; exit 1; fi
find /ucrt64/lib/gstreamer-1.0 -maxdepth 1 -type f \( -iname "*.dll" -o -iname "*.dll.a" \) -iname "*.dll" -exec cp -p "{}" "$stage/lib/gstreamer-1.0/" \;
plugin_count="$(find "$stage/lib/gstreamer-1.0" -maxdepth 1 -type f -iname "*.dll" | wc -l)"
[[ "$plugin_count" -gt 0 ]] || { echo "No GStreamer modules found" >&2; exit 1; }
echo "Packaged GStreamer plugins: $plugin_count"
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
