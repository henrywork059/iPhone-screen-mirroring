# iPhone Mirror — threat model (mirroring only)

**Status:** living document for source review; no independent security sign-off.

## Assets to protect

- Confidentiality of the iPhone's mirrored video, audio and pairing PIN.
- Integrity and availability of the Windows device, user files and OS.
- Integrity of UxPlay, GStreamer, OpenSSL, libplist, their native DLLs and
  the GitHub Actions build/release pipeline.
- Preservation of Windows Smart App Control, Windows Firewall and Defender.

## Entry points and trust boundaries

1. **Local network:** unauthenticated discovery and pre-authentication RTSP
   or RTP requests cross from the LAN into the receiver's native C parsing
   code. A Private network category does not authenticate the sender.
2. **Paired AirPlay session:** authentication can restrict session access but
   does not make media headers, encrypted frames, codec records or plists
   inherently trusted. Continue validating all lengths after pairing.
3. **Native plugin pipeline:** decoded media crosses between UxPlay and
   GStreamer plugins/FFmpeg libraries. Enforce a private plugin path and
   verify native executable hashes. Minimize available elements.
4. **Build supply chain:** patched pinned UxPlay source and MSYS2 packages are
   downloaded to CI; source revision and action commit hashes are pinned, but
   binary dependency package versions are not yet fully locked.
5. **Windows UI to process:** validated receiver name, explicit consent and
   checksum-bound executable are necessary defenses, not OS sandboxing.
6. **Windows executable trust:** source/ZIP checksums do not authenticate the
   publisher. Smart App Control may block unsigned Windows processes.

## Attacker goals and corresponding controls

| Threat | Present defenses | Residual risk |
|---|---|---|
| Untrusted LAN client sends malformed frames | Packet ceiling, frame/codec bounds patch, regression tests | Other RTSP/mDNS/audio handlers unreviewed |
| Large frames exhaust memory | 32 MiB per-frame ceiling and controlled allocation failure | Multiple sessions, CPU exhaustion and aggregate resource limits |
| Forged or replayed AirPlay pairing | Random pairing PIN and existing protocol authentication | Need rate-limit/authentication review and end-to-end tests |
| Replace native DLL to gain code execution | Private PATH/plugin paths and SHA-256 code inventory | Unsigned GUI, same-user filesystem access, no sandbox |
| Native codec exploit | 16 allowlisted GStreamer plugins, minimum 1.28.8 | Full codec and dependency vulnerability audit pending |
| Unsafe build action or dependencies | SHA-pinned Actions and UxPlay source, package inventory | Rolling third-party packages and no signed build provenance |
| Trick user into disabling Windows protection | Unsigned distribution hold, warnings and docs | Historical experiment artifacts may remain accessible |
| Public network exposure | Private network and Firewall inbound-default checks | Existing broad allow rules, other interfaces and trusted-LAN attackers |
| Theft of mirrored screen contents | User opt-in and volatile controller log | Screen/video shown on PC; host and network trust still required |

## Verification scope

Automated checks exercise patch application, native builds, bounds-check
helpers under ASan/UBSan, a coverage-guided primitive fuzzer, GStreamer
element discovery, and source-level Windows release guards. There is no
complete protocol fuzzing, no malicious-client integration test, no device
test on an isolated Windows host, no independent security review, and no
trusted certificate available to sign a release.

**Security policy:** stay mirroring-only. Keep the default receiver security
hold. Do not upload unsigned artifacts, disable Smart App Control, add a
Defender exclusion, or open broad firewall rules to bypass testing.
See [security hardening status](SECURITY_HARDENING_STATUS.md) for open gates.
