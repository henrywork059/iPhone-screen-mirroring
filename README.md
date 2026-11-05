# iPhone Screen Mirroring for Windows

A local-first iPhone-to-Windows 11 AirPlay mirroring project.

> **EXPERIMENTAL TEST BUILD AVAILABLE (2026-11-05):** An explicitly opt-in Windows ZIP now includes a patched UxPlay receiver and a controller that can start it for actual iPhone AirPlay mirroring. [Download from the successful GitHub Actions build](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/experimental-airplay-beta.yml). Follow [experimental setup and risk warnings](docs/EXPERIMENTAL_AIRPLAY_TEST.md). **Real-device functionality is not yet confirmed.**
>
> **STABLE RELEASE SECURITY HOLD:** Default application builds and PowerShell startup remain disabled. The old UxPlay v1.73.7 release has a reported heap-buffer overflow ([issue #614](https://github.com/FDH2/UxPlay/issues/614)). The opt-in beta uses a separately pinned 1.74 experimental revision plus our regression-tested correction, but the complete AirPlay/mDNS network parser remains **unaudited**. No stable installer is available. Do not use for sensitive content or on an untrusted network.

## Offline Windows UI preview (available for user testing)

The **UI-only** self-contained Windows 11 preview is available as a short-lived GitHub Actions artifact: [successful Windows preview run](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41978061900). Download the `iPhoneMirror-Offline-UI-Preview` artifact and extract the contained `offline-controller.zip`.

It **does not contain UxPlay**, cannot mirror an iPhone, and will **not open an AirPlay listener**. Confirm that the window opens and clearly shows the disabled **Launch blocked** control. See [safe UI test instructions](docs/OFFLINE_CONTROLLER_PACKAGE.md). Do not bypass Windows security warnings for unsigned binaries.

## Actual iPhone mirroring: controlled test build

**Use [the experimental AirPlay test guide](docs/EXPERIMENTAL_AIRPLAY_TEST.md), not the old stable installation instructions.** Download the explicitly named `iPhoneMirror-Experimental-AirPlay-TEST` artifact from the [beta workflow](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/experimental-airplay-beta.yml), extract both ZIP layers, then run `MirrorControl.exe` as a standard user on an isolated trusted Private network. Confirm the experiment warning and choose the receiver on your iPhone under Control Center → Screen Mirroring. The PIN appears in UxPlay's separate console, and video appears in UxPlay/GStreamer's separate window.

This **can open listening network ports**. No full security audit, real iPhone acceptance test or signed installer exists. **Do not bypass SmartScreen or antivirus warnings.** The default controller remains locked.

## Current validation results (2026-11-05)

- **PASSED:** full patched experimental Windows UxPlay source build; no network listener was started.
- **PASSED:** Linux ASan/UBSan and Windows UCRT64 C regression tests, including real OpenSSL AES-CTR/SHA-512.
- **PASSED:** self-contained WinForms GUI packaging inspection.
- **PASSED:** unpublished combined bundle inspection with 241 GStreamer plugins, 208 runtime DLLs and SHA-256 inventories; approximately 166 MB ZIP.
- **NOT DONE:** real-device screen mirroring, native runtime behavior on a clean Windows laptop, firewall isolation, complete security review and signed installer.
- **DEFAULT SECURITY HOLD ACTIVE:** a dedicated opt-in experimental mirroring test bundle is available, but there is no approved stable release.

See [full Windows build](docs/FULL_RECEIVER_BUILD.md), [unpublished bundle inspection](docs/PRETEST_BUNDLE.md), [dependency inventory](docs/DEPENDENCY_INVENTORY.md), and the [device-test handoff](docs/DEVICE_TEST_HANDOFF.md). The next user-required action is physical test validation **only after** a reviewed test package is approved; do not execute current experimental binaries.

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
- iPhone remote control and DRM-protected video are not supported.

## Project documentation

- [Windows setup and build — blocked](docs/WINDOWS_SETUP.md)
- [Security review and unblock criteria](docs/SECURITY_REVIEW.md)
- [Architecture decisions](docs/ARCHITECTURE_DECISIONS.md)
- [Manual test plan](tests/MANUAL_TEST_PLAN.md)
- [Existing design docs](iphone-windows-mirror/docs/ARCHITECTURE.md)
- [Existing security goals](iphone-windows-mirror/docs/SECURITY.md)
- [Original roadmap](iphone-windows-mirror/docs/ROADMAP.md)

## Licensing

The original controller/launcher code has **no license selected yet**; do not assume a license grant. UxPlay is GPL-3.0 and must be obtained separately from its upstream repository. Before any combined distribution, review GPL-3.0 obligations and third-party component licenses. No UxPlay source, executables, or GStreamer DLLs are vendored here.
