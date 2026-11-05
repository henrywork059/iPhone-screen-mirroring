# Windows receiver build dependency inventory

**Build-only experimental snapshot, November 5, 2026. Not a release approval or installer manifest.**

## Upstream source

- Repository: https://github.com/FDH2/UxPlay
- Pinned experimental commit: `7aaad6222c2e136723e6e3d25c3be784260c577d` (not a stable version).
- Local experimental patch: [`patches/uxplay-experimental-mirror-short-frame.patch`](../patches/uxplay-experimental-mirror-short-frame.patch).
- Build workflow: [Windows full receiver build](../.github/workflows/full-receiver-build.yml).
- Reference [successful CI run](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41974814695).

## Reported build packages

| Component | MSYS2 UCRT64 version |
| --- | --- |
| GCC | 16.2.0-6 |
| CMake | 4.4.5-1 |
| libplist | 2.7.0-4 |
| GStreamer | 1.28.8-1 |
| OpenSSL | 3.6.5-1 |

The CI-generated experimental `uxplay.exe` had an **informational SHA-256** of `33822c578deb442eefee8b93fd825a14dfc2c232a5e8abd7d28d4403b54c7614`. It was not uploaded or released. The hash is not a signature, not an installer checksum and not an approved receiver allowlist entry.

### Direct non-Windows imports reported

`libgcc_s_seh-1.dll`, `libwinpthread-1.dll`, `libstdc++-6.dll`, `libcrypto-3-x64.dll`, `libglib-2.0-0.dll`, `libgobject-2.0-0.dll`, `libgstapp-1.0-0.dll`, `libgstreamer-1.0-0.dll`, `libgstvideo-1.0-0.dll`, `libplist-2.0.dll`. Windows system DLLs are omitted from this list.

**Important:** The direct-import list does not include transitive DLL dependencies or dynamically loaded GStreamer plugins. It is insufficient to create a portable runtime. Those dependencies, licensing notices, build provenance and redistributability require separate verification.

## Still needed for distribution

- Record the complete GStreamer plugin inventory and transitively imported native libraries.
- Record SHA-256 hashes of all redistributable files and their exact source package IDs.
- Include license notices, GPL-3.0 source availability and applicable third-party source offers.
- Review the experimental mDNS implementation and UxPlay protocol parsing; complete isolated network testing.
- Code signing and a usable installer remain unimplemented.

## Selected vulnerability disclosures reviewed (2026-11-05)

Published MSYS2 vulnerability lists can include **historical CVEs** even when the current package is beyond the upstream fixed version. The selected versions were compared with the maintainers' advisories:

- GStreamer-SA-2026-0047 (CVE-2026-12892, H.264 parser): fixed in GStreamer **1.28.5**. Current build used **1.28.8**: https://gstreamer.freedesktop.org/security/sa-2026-0047.html
- GStreamer-SA-2026-0048 (CVE-2026-12891, H.266 parser): fixed in **1.28.5**. Current build used **1.28.8**: https://gstreamer.freedesktop.org/security/sa-2026-0048.html
- GStreamer-SA-2026-0072 (CVE-2026-73433/73434, AVI demuxer): fixed in **1.28.6**. Current build used **1.28.8**: https://gstreamer.freedesktop.org/security/sa-2026-0072.html
- Older libplist CVE-2017-5834/5835/5836 entries are reported for ancient versions. Debian reports fixed versions predating 2.7.0, but that is not a substitute for directly verifying the exact MSYS2 binary build: https://security-tracker.debian.org/tracker/CVE-2017-5836

This checks **selected known advisories only**; it is not a comprehensive dependency-security audit. The UxPlay short-frame finding and unverified experimental mDNS code remain separate blockers.
