# Security hardening status — 2026-11-06

**STATUS: NOT APPROVED FOR INSTALLATION OR RELEASE.**

This is an internal source-level hardening report for the mirroring-only
AirPlay Windows prototype. No independent security audit, formal certification,
trusted code signature or complete end-to-end testing is claimed.
**Keep Windows Smart App Control, Defender and Windows Firewall enabled.**

## Additional work completed November 6, 2026

- Patched the **actual pinned UxPlay HTTP/reverse-HTTP parser** with URL, header,
  and body size limits; bounded event Content-Length parsing; and corrected
  leading-byte offsets. File: `patches/uxplay-http-request-bounds.patch`.
- Added `tests/native/uxplay_http_regression.c` and
  `tests/native/uxplay_http_fuzz.c`, compiled against upstream
  `http_request.c` and its bundled `llhttp`. Linux ASan/UBSan regression
  and 50,000-run coverage fuzz workflow passed:
  [run 42129275207](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42129275207).
- Added mDNS compression-name regression and coverage fuzzing using the
  actual upstream `mdnsd.c`. Linux sanitizer job passed:
  [run 42129631917](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42129631917).
- The patched HTTP network-receiver Windows build completed successfully:
  [run 42128958672](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42128958672).
- Exact version checks added for nine primary MSYS2 GStreamer, FFmpeg,
  OpenSSL, libplist and JSON dependencies. The build now **fails closed**
  on unreviewed version drift; it is not a complete dependency lock or
  independently verified supply-chain provenance.
- The default receiver and signed-release security holds remain active.
  A manually requested unsigned ZIP may be downloaded for diagnostic
  inspection, without overriding Smart App Control or release approval.

**Important limits:** these tests do not fuzz complete paired AirPlay
sessions, all libplist/audio/crypto handling or all DNS record parsing.
A trusted publisher certificate, independent audit and clean-machine
real-device/firewall tests remain mandatory and unavailable in this
repository-only workflow. Do not label a build production safe on CI success.

## Changes implemented

1. **Network frame validation:** the candidate AirPlay backend still pins
   FDH2/UxPlay commit `7aaad6222c2e136723e6e3d25c3be784260c577d`.
   Its original short-frame fix remains in place. A separate exact-source
   patch `patches/uxplay-mirror-rtp-bounds.patch` adds a maximum compressed
   frame size (32 MiB), validates H.264/H.265 parameter set offsets and
   lengths, checks NAL-unit length before dereferencing its content, rejects
   malformed floating-point dimensions, handles empty frames/invalid plists
   and avoids process-wide exit on allocation failure. An exact-match patch
   is applied and checked in the build. This does not address every possible
   bug in the complete UxPlay codebase.

2. **Memory-safety tests:** `tests/native/mirror_validation_regression.c`
   exercises the same validation header used in the backend against 250,000
   hostile inputs. It runs under ASan/UBSan on Ubuntu and as a separate
   Windows native regression. `tests/native/mirror_validation_fuzz.c`
   uses Clang's coverage-guided libFuzzer with sanitizers for 60,000 runs
   in CI. These test bounded frame and codec primitives only; neither
   substitutes for comprehensive RTSP, mDNS, media pipeline and transport
   protocol fuzzing.

3. **Decoder/plugin reduction:** `stage-native-runtime.sh` selects only
   providers for the UxPlay mirroring audio/video elements, validates the
   relocated plugins in isolation and refuses to stage unexpected numbers.
   An earlier full run reported 241 plugins and 208 DLLs; the minimized
   validation run reported **16 plugins and 125 native DLLs**. Real-device
   rendering after this reduction has not been confirmed.

4. **GStreamer security baseline:** production of a candidate requires
   GStreamer >= 1.28.8, which includes fixes for October 2026 security
   advisories. Individual FFmpeg/codec DLLs and OpenSSL must still undergo
   vulnerability and transitive dependency review.

5. **Windows preflight:** `NetworkGuard` checks the Private profile,
   additionally requires Windows Firewall enabled with default inbound
   blocking on both Private and Public. This fails closed on missing or
   unknown firewall status. **This is not interface isolation or proof that
   a permitted LAN client cannot reach the listener**; it does not audit all
   port-level and third-party firewall policies.

6. **Native-file integrity:** the experimental GUI checks the receiver's
   build SHA-256 and the build-time SHA-256 of its `SHA256SUMS.txt` inventory.
   Before launching, it verifies the included native files and rejects
   executable DLLs/EXEs not listed. A checksum bound to an unsigned GUI is
   useful for tamper detection, **not publisher authentication**.

7. **Build supply chain:** GitHub Actions references were changed from
   movable major-version labels to exact verified commit SHAs.
   The workflow records the installed MSYS2 package inventory and produces
   a deterministic **CycloneDX 1.5 file-level native-code SBOM**. Package
   dependencies are still resolved from a rolling MSYS2 repository; their
   versions are recorded but not fully locked/reproducible.

8. **Controlled experimental distribution:** normal builds keep
   `BackendSecurityGate.IsBlocked => true`; the PowerShell launcher is
   also blocked. Automatic CI and offline-controller builds **do not upload
   unsigned executable ZIPs**. The experimental workflow can upload an
   **unsigned, manually triggered ZIP** retained for
   three days for inspection or limited testing. This is not an approved
   release or exception to Smart App Control. Do not bypass Windows
   warnings or use an unsigned prototype for routine installation.

9. **Scope reduction:** no BLE HID, ESP32, touch injection, mouse/keyboard
   capture, driver installation, service autostart, privileged installer or
   other remote-input mechanism is included.

## Release gates that remain OPEN

- **Full upstream audit** of the UxPlay RTSP, mDNS, pairing, RTP mirror and
  audio handlers, libplist, crypto error handling and codec boundaries;
  independent review of high-risk changes, plus upstream coordination.
- **Protocol-level fuzzing** of full network and media parsing, including
  fragmented, reordered and malformed AirPlay traffic, with ASan/UBSan and
  coverage reports; regression evidence for all discovered bugs.
- **Runtime confinement** and Windows Firewall rule validation on an
  isolated PC with an explicitly controlled iPhone sender, not just Private
  profile checks. Examine any broad program or port allow rules.
- **Clean Windows 11 + real iPhone tests** covering discovery, PIN pairing,
  audio/video, sleep/reconnect, 1080p and HEVC, firewall behavior, and
  reliable shutdown. The native minimized plugin set must be exercised.
- **Supply-chain review:** complete binary-to-package SBOM mapping,
  vulnerability scanning with manually checked advisories, reproducible
  version-locking, provenance attestations and third-party license audit.
- **Independent penetration testing and threat-model sign-off**, followed
  by a reviewed security fix process and ongoing dependency maintenance.
- **Trusted publisher signing:** obtain an approved Authenticode certificate
  or Microsoft Artifact Signing identity, then sign and verify all delivered
  EXEs/DLLs and any installation code. A self-signed certificate, hashes,
  Windows exclusions or Smart App Control deactivation are **not** accepted
  release solutions.
- **Final release authorization:** only after the above checks pass should
  a gated signed release workflow or Microsoft Store MSIX be enabled.

## Controls and evidence

- [Offline UxPlay sanitizer workflow](../.github/workflows/uxplay-memory-safety.yml)
- [Coverage-guided bounds fuzzer](../.github/workflows/uxplay-security-fuzz.yml)
- [Native CI, manual-only unsigned diagnostic artifact](../.github/workflows/experimental-airplay-beta.yml)
- [Native staged runtime dependency inspection](../.github/workflows/pretest-bundle.yml)
- [Security hold and vulnerability reporting](../SECURITY.md)
- [Microsoft Smart App Control signing guide](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)

**Do not disable Smart App Control or bypass a security warning.**
For regular use, prefer an established, signed mirroring receiver while
this prototype remains under review.
