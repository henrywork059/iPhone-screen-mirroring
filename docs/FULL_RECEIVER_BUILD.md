# Complete Windows receiver build — experimental, build-only

CI builds the **full UxPlay executable**, rather than just its AES code. It checks out a specific upstream experimental commit `7aaad6222c2e136723e6e3d25c3be784260c577d` (1.74 development branch, not a stable release), applies the local [short-frame correction](../patches/uxplay-experimental-mirror-short-frame.patch), executes **isolated local C regressions** with deterministic and real OpenSSL crypto, then builds the whole receiver with MSYS2 UCRT64, CMake, Ninja, GStreamer and the experimental internal mDNS implementation.

This path is intentionally separate from the **historically blocked UxPlay v1.73.7**. It uses the development branch exclusively to investigate full Windows build feasibility because v1.73.7 expects the Apple Bonjour SDK.

## Safety restrictions

- No AirPlay listener is launched by CI; the executable is only compiled.
- No executable or runtime DLLs are uploaded or released.
- The project Windows controller and PowerShell launchers remain blocked.
- A build success is not evidence that this experimental branch is secure or compatible with your iPhone.
- The generated checksum and imported DLL names are recorded in CI logs for development only; they do not establish provenance of a release.

## Acceptance criteria for this stage

- Full Windows source builds successfully at exactly the pinned upstream commit with the local patch.
- Both native C regression suites execute without errors.
- Third-party package versions and native DLL imports are printed.
- Windows controller still compiles and its security-hold CI checks remain enabled.

Next: dependency review, independent protocol/codec review and an isolated network configuration before supervised real-device testing.
