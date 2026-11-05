# iPhone Screen Mirroring for Windows

A privacy-first project to mirror an iPhone screen and play audio on Windows 11 over a trusted local network.

> **Status: planning / initial scaffold.** No working receiver or Windows installer is included yet.

## Goal

Connect from **iPhone → Control Center → Screen Mirroring** to a Windows PC, without installing an app on the iPhone. The intended Windows application will support video, audio, a resizable window, fullscreen, and clear connection controls.

## Design requirements

- **Local first:** no cloud account, tracking, or remote relay.
- **Safe defaults:** PIN pairing enabled; no public-network firewall exception by default.
- **Least privilege:** avoid requiring administrator access for normal use.
- **Transparent builds:** pinned and reviewed dependencies; documented build steps and checksums.
- **Practical limitations:** remote iPhone control is out of scope; protected/DRM content may not mirror.

## Technical approach

We will first evaluate [UxPlay](https://github.com/FDH2/UxPlay) as the AirPlay receiver foundation. A native Windows UI may control a separately running receiver at first, before any deeper integration. This is a **proposal**, not an implemented or security-audited application.

UxPlay is GPL-3.0 licensed. Any integration and distribution must comply with its license and third-party notices. The project's own final licensing decision is still open.

## Project documents

The initial starter materials were uploaded in [`iphone-windows-mirror/`](iphone-windows-mirror/):

- [Project overview](iphone-windows-mirror/README.md)
- [Architecture proposal](iphone-windows-mirror/docs/ARCHITECTURE.md)
- [Security requirements](iphone-windows-mirror/docs/SECURITY.md)
- [Development roadmap](iphone-windows-mirror/docs/ROADMAP.md)

## Next milestone

Validate a current, patched UxPlay build on Windows 11 with an iPhone on the same private network. Verify PIN pairing, firewall configuration, audio/video behavior, and license obligations **before** wrapping it in a custom interface.

No unreviewed installers, credentials, pairing keys, crash dumps, or third-party build artifacts should be committed.
