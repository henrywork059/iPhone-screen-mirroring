# Prototype architecture decisions

## Separation of concerns

- **`src/MirrorControl`:** .NET 8 Windows Forms shell. Chooses the UxPlay executable and receiver name; checks network-profile policy; starts and stops the child process.
- **`config/uxplayrc`:** intentionally empty startup configuration passed explicitly with `-rc`. Prevents unexpected user-level UxPlay startup options from being loaded. The launcher passes its security options on the command line.
- **External UxPlay v1.73.7:** handles Bonjour discovery, AirPlay handshake, PIN exchange, media transport and playback through GStreamer. Upstream source is not copied into this repo.
- **`scripts/Start-Mirroring.ps1`:** standalone executable frontend for troubleshooting or use without the .NET GUI.
- **`scripts/Check-Prerequisites.ps1`:** explains where UxPlay is expected and reports physical network profiles.

## Why use a separate UxPlay console?

UxPlay prints the pairing PIN using standard C output, and that output may be buffered when redirected to a GUI process. The prototype therefore launches a **visible console** for UxPlay. The controller does not claim to display a PIN unless it can do so reliably. Future GUI integration may use a purpose-built protocol adapter or a reviewed upstream change.

## Network policy

Both paths refuse to start unless at least one connected physical Windows network profile exists and all those profiles are `Private`. The GUI invokes built-in Windows PowerShell cmdlets without elevation. Neither route changes the firewall.

This is **not** network confinement: Windows category settings can be incorrect; multiple interfaces and existing firewall rules can create paths not covered by these checks. Before any public release we need a narrower, reviewed inbound firewall configuration and explicit exposure testing.

## Process control

The controller launches `uxplay.exe` from a user-specified path, with explicitly enumerated arguments, and does not download or bundle binaries. Windows uses the executable directory for runtime DLLs. The GUI Stop button currently uses process-tree termination. It is a **known limitation** that shutdown may not be graceful and media sessions may be abruptly interrupted.

## Deferred

- Integrated video playback, fullscreen and quality controls in our own UI
- Reliable PIN rendering inside the UI
- Port selection and accurate firewall rule enforcement
- Build/installer artifacts, package signing and dependency audits
- Cross-platform support, iPhone remote control, protected-content mirroring
