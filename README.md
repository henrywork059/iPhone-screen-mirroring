# iPhone Screen Mirroring for Windows

A local-first iPhone-to-Windows 11 AirPlay mirroring project.

> **SECURITY HOLD (November 5, 2026): Do not run the AirPlay receiver or perform device/network testing yet.** UxPlay v1.73.7 has a reported heap-buffer overflow in `lib/mirror_buffer.c` (upstream [issue #614](https://github.com/FDH2/UxPlay/issues/614)). Both project launch paths are intentionally disabled until a patched backend is independently reviewed and approved. See [security review](docs/SECURITY_REVIEW.md).

> **Prototype source available; Windows/iPhone end-to-end test pending.** This repository does **not** include an AirPlay engine, prebuilt installer, or verified working release.

## First prototype

- **Controller:** .NET 8 Windows Forms app under [`src/MirrorControl`](src/MirrorControl/).
- **Receiver:** upstream [UxPlay](https://github.com/FDH2/UxPlay). Formerly pinned v1.73.7 is **not approved for execution** due to a subsequent heap-overflow report.
- **Video/audio:** UxPlay + GStreamer opens a separate video window and uses local Windows audio. The controller does not yet embed video.
- **Pairing:** UxPlay `-pw` without a password requests a **new random 4-digit code on every connection**; its **console** displays the code.
- **Network guard:** both launchers refuse to start unless all detected connected physical Windows profiles are Private. This is a safety check, **not** a firewall or a security boundary.
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
