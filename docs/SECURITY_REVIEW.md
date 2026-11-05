# Security review — HOLD on running receiver

**Status:** BLOCKED on November 5, 2026. **Do not use this prototype for iPhone-to-Windows AirPlay mirroring yet.**

## Finding: heap buffer overflow in UxPlay v1.73.7

- **Upstream report:** https://github.com/FDH2/UxPlay/issues/614
- **Upstream fix:** https://github.com/FDH2/UxPlay/commit/b1be2bd26629f822fa1f6ebbdb0c3a2ed30aa62f
- **Affected component:** `lib/mirror_buffer.c`, `mirror_buffer_decrypt()`
- **Affected version:** the published v1.73.7, which our project previously specified.
- **Impact:** a video packet smaller than the AES-CTR leftover keystream can cause out-of-bounds reads/writes and loss of synchronization. The upstream report demonstrates a heap-buffer-overflow under AddressSanitizer, in a third-party AirPlay sender scenario. A malicious or buggy sender reachable over the LAN could potentially trigger related behavior; precise exploitability and reachable conditions are **not independently established** here.
- **Upstream status:** the issue was marked fixed November 5, 2026 and changes landed on the development branch, **not** in a published replacement stable release as of this review.

The earlier security fixes in UxPlay 1.73.7 (GHSA-479c-ww7g-wgp8, CVE-2025-60458) do not address this newly reported defect.

### Additional review concern

The development-branch patch contains `for (int i = 0; i < mirror_buffer->nextDecryptCount; i++)` with an early return when `inputLen` is shorter. This change warrants a dedicated regression test for frames shorter than the residual keystream. A closed upstream issue does **not** establish that the patch has been audited and tested on Windows.

## Our controller's security posture

**Positive controls implemented:** source-only .NET/PowerShell launchers, random PIN required on each connection, refusal to start on non-Private physical network profiles, no automatic downloads or firewall rules, no telemetry in our code.

**Limitations:** a Private profile is a user-configured label, not enforced network isolation. Virtual interfaces and existing firewall rules can still expose the receiver. The project cannot attest that a user-selected `uxplay.exe` is genuine, patched, or built reproducibly. It also does not validate all third-party native DLLs or run protocol fuzzing. Compilation and syntax checks are **not** security tests.

**Mitigation now:** the GUI Start button and its Start handler, and the PowerShell launch script, are disabled. Historical UxPlay build instructions are labeled not approved.

## Criteria before lifting the block

1. A fixed, published upstream version or a specifically reviewed patch set with audited source, pinned commit and verified dependencies. Do **not** switch to arbitrary `master` automatically.
2. Regression coverage for short video frames, AES-CTR residual handling, and prior security issues, including tests with sanitizers where practical.
3. Independent static review of the launchers and receiver configuration; no unknown external binaries or automatic Public-network exceptions.
4. Real Windows firewall and network exposure tests, first on a controlled, isolated trusted LAN, followed by supervised iPhone functional tests using non-sensitive content.
5. Update setup instructions, application version verification and CI safety checks. Record results before any release or recommendation to install.

## Progress — offline regression completed

As of November 5, 2026, our repository contains an **experimental, source-only fix** at [`patches/uxplay-mirror-short-frame.patch`](../patches/uxplay-mirror-short-frame.patch). It is applied to pinned UxPlay v1.73.7 **only in an isolated CI test**, never by the Windows launchers.

- **Passed:** short/empty/boundary video-frame regression against the real upstream C module with deterministic crypto test doubles, under ASan/UBSan.
- **Passed:** same categories of regression against the real UxPlay OpenSSL AES-CTR/SHA-512 implementation, under ASan/UBSan.
- **Passed:** Windows C# controller build and existing fail-closed launcher checks.
- **Not performed:** Windows native UxPlay compilation, full network-protocol fuzzing, installer audit, dynamic firewall isolation, or real iPhone tests.

Upstream's current development fix differs from our candidate: its short-frame branch checks for partial input **inside** a loop still bounded by the original residual length. Our source review identifies a correctness risk for partial input and a possible out-of-bounds access for empty input. Upstream review and independent Windows validation remain necessary.

CI jobs: [offline memory-safety test](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/uxplay-memory-safety.yml) and [Windows source checks](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/windows-checks.yml).

**No change to the security hold:** successful targeted regressions do not establish that the complete AirPlay receiver is safe.
## What remains safe to do

- Review source code and requirements.
- Improve the controller's UI and tests without executing a network listener.
- Build/check the **controller only** with .NET 8 and GitHub Actions.
- Open issues and prepare a future test plan.

This is not a formal security audit, malware certification or assurance of zero vulnerabilities.
