# iPhone Screen Mirroring for Windows

A local-first iPhone-to-Windows 11 AirPlay mirroring project.

> **SECURITY HOLD — NO APPROVED WINDOWS DOWNLOAD (2026-11-06):**
> The native mirroring-only prototype is being hardened. GitHub Actions
> compiles it and runs offline security tests, but no new unsigned executable
> artifacts are uploaded. Earlier experiment ZIPs may still exist in old
> Actions runs; they are **not approved for installation**, especially with
> Smart App Control enabled. Do not bypass Windows Security.
> [Current hardening evidence and release gates](docs/SECURITY_HARDENING_STATUS.md).
>
> **STABLE RELEASE SECURITY HOLD:** Default application builds and PowerShell startup remain disabled. The old UxPlay v1.73.7 release has a reported heap-buffer overflow ([issue #614](https://github.com/FDH2/UxPlay/issues/614)). The opt-in beta uses a separately pinned 1.74 experimental revision plus our regression-tested correction, but the complete AirPlay/mDNS network parser remains **unaudited**. No stable installer is available. Do not use for sensitive content or on an untrusted network.

## Offline Windows UI preview (available for user testing)

The **UI-only** self-contained Windows 11 preview can be built locally or in CI for offline inspection. Unsigned preview uploads are now disabled. Previously produced unsigned artifacts are not approved for installation while Smart App Control is enabled.

It **does not contain UxPlay**, cannot mirror an iPhone, and will **not open an AirPlay listener**. Confirm that the window opens and clearly shows the disabled **Launch blocked** control. See [safe UI test instructions](docs/OFFLINE_CONTROLLER_PACKAGE.md). Do not bypass Windows security warnings for unsigned binaries.

## Actual iPhone mirroring: security hold

The project implements a local Windows AirPlay mirroring receiver, but it
is **not approved for routine use or installation**. CI builds the patched
UxPlay backend for controlled security verification only, without making
the unsigned package available for download. Smart App Control must remain ON.
A trusted code-signing identity, independent audit and real-device/isolated
network validation are pending. See [the security status](docs/SECURITY_HARDENING_STATUS.md).

## Higher-quality mirroring profiles (November 2026)

- Select **1080p/30, 1080p/60, 1440p/60 (default), or 4K/30** in the controller before starting a mirror session. HEVC and real-device support are experimental; the iPhone can negotiate a lower resolution or frame rate.

## Smart App Control and mirroring-only builds

**Windows Smart App Control stays ON.** Microsoft does not provide per-app
exceptions. Our experimental Windows package is **unsigned** because there
is no trusted code-signing identity configured for this project.
An unsigned mirroring-only app may still be blocked by Smart App Control.
**Removing Bluetooth does not make the receiver or controller trusted.**
Do not disable Smart App Control, bypass Windows Security warnings, or add
an antivirus exclusion.

A compatible future build requires a reviewed application and a valid
trusted code-signing certificate for its executable components.
See Microsoft's [Smart App Control signing guidance](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control).

**Mirroring-only:** Bluetooth HID, ESP32, keyboard/mouse capture, remote
control and touchscreen input have all been removed. The app retains AirPlay
video/audio mirroring, quality presets, consent prompts and the default
security hold.

## Current validation (November 2026)

- **PASSED:** source-level Windows build and help-only native runtime checks.
- **PASSED:** ASan/UBSan bounded-frame regressions and coverage-guided
  fuzzing of the new frame/codec validation helpers.
- **PASSED:** GStreamer plugin allowlist verification — 16 plugins, reduced
  from 241; native DLL count reduced from 208 to 125.
- **IMPLEMENTED:** Private/Public Windows Firewall default inbound checks,
  native dependency SHA-256 inventory verification, pinned GitHub Actions,
  a file-level CycloneDX SBOM and a hold on unsigned artifact uploads.
- **NOT COMPLETED:** full network protocol fuzzing, independent security
  assessment, trusted signing, real iPhone tests, complete runtime isolation,
  dependency reproducibility and release approval.

Read [Security hardening status](docs/SECURITY_HARDENING_STATUS.md)
for the exact remaining requirements. CI passing does not imply safety.

## First prototype

**Offline progress:** Proposed short-frame fix has passed Linux AddressSanitizer/UndefinedBehaviorSanitizer tests and Windows UCRT64 native regression tests, including real OpenSSL cryptography. This is *not* a full receiver audit; all AirPlay launch paths remain blocked. [Validation results](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41960621529).

- **Controller:** .NET 8 Windows Forms app under [`src/MirrorControl`](src/MirrorControl/).
- **Receiver:** upstream [UxPlay](https://github.com/FDH2/UxPlay). Formerly pinned v1.73.7 is **not approved for execution** due to a subsequent heap-overflow report.
- **Video/audio:** UxPlay + GStreamer opens a separate video window and uses local Windows audio. The controller does not yet embed video.
- **Pairing:** UxPlay `-pw` without a password requests a **new random 4-digit code on every connection**; its **console** displays the code.
- **Network guard:** experimental GUI requires connected physical and virtual network profiles to be Private; the default PowerShell launcher remains blocked. This is a safety check, **not** a firewall or a security boundary.
- **No automatic downloads, firewall rules, background services, or telemetry** in the controller or launch script.

## Windows setup (currently blocked)

**STOP:** The following steps are historical prototype instructions and **must not be performed until the security hold is cleared**. The GUI launch button and PowerShell startup script intentionally refuse to start UxPlay.

### After a patched release is approved

1. Follow [Windows setup](docs/WINDOWS_SETUP.md) to install dependencies and compile upstream UxPlay. Only the UxPlay build needs MSYS2; the controller needs .NET 8.
2. Connect iPhone and laptop to the same **trusted** Wi-Fi or LAN and use Windows' **Private** network profile. Review any existing inbound rules for UxPlay.
3. From the root of this repository, run `powershell -NoProfile -File .\scripts\Check-Prerequisites.ps1`.
4. Choose **one**:
   - **GUI:** `dotnet run --project .\src\MirrorControl\MirrorControl.csproj`. Click **Start receiver**. The UxPlay PIN is shown in its separate console.
   - **Terminal only:** `powershell -NoProfile -File .\scripts\Start-Mirroring.ps1`. The PIN appears in this terminal.
5. On iPhone: **Control Center → Screen Mirroring → iPhone Mirror**, then enter the displayed PIN.
6. Allow Windows Defender Firewall **Private** access for the UxPlay executable if prompted, **not Public**. Stop from the controller or with Ctrl+C in the terminal.

If the UxPlay executable is in another location, change the GUI's path field or pass `-UxPlayPath` to the script.

## Safety and privacy limits

- The PIN is shown in a console; do not screen-share or record that console while pairing.
- There is **no independent security audit** and **no Windows/iPhone validation** yet. Review the original UxPlay code and dependencies before trusting sensitive screen content.
- Network category is user-configurable; use a trusted LAN, and verify firewall rules manually. Existing broad firewall exceptions are not removed.
- Start/stop from the GUI currently terminates UxPlay immediately; graceful shutdown remains a TODO.
- No Bluetooth HID, external hardware input, remote control, direct touch, multitouch or DRM-protected video is supported.

## Project documentation

- [Windows setup and build — blocked](docs/WINDOWS_SETUP.md)
- [Security review and unblock criteria](docs/SECURITY_REVIEW.md)
- [Architecture decisions](docs/ARCHITECTURE_DECISIONS.md)
- [Manual test plan](tests/MANUAL_TEST_PLAN.md)
- [Existing design docs](iphone-windows-mirror/docs/ARCHITECTURE.md)
- [Existing security goals](iphone-windows-mirror/docs/SECURITY.md)
- [Original roadmap](iphone-windows-mirror/docs/ROADMAP.md)

## Licensing

The original controller/launcher code has **no license selected yet**; do not assume a license grant. UxPlay is GPL-3.0 and must be obtained separately from its upstream repository. Before any combined distribution, review GPL-3.0 obligations and third-party component licenses. No UxPlay binaries or GStreamer DLLs are vendored in Git. The unpublished CI candidate includes corresponding UxPlay source and dependency license notices.
