# iPhone Windows Mirror

A privacy-first, local-network iPhone-to-Windows 11 screen mirroring project.

> **Project status: planning / initial scaffolding.** This repository does not yet contain a working AirPlay receiver.

## Product goal

Display an iPhone's screen and play its audio in a native Windows 11 application using iOS **Control Center → Screen Mirroring**, without requiring an app on the iPhone.

## Guiding principles

- Local network only; no cloud account, telemetry, or remote relay.
- PIN-based pairing enabled by default.
- Restrict inbound firewall exposure to private networks.
- Avoid unnecessary administrator privileges.
- Provide fullscreen, resizable window, and optional always-on-top mode.
- Prefer maintained, audited dependencies with pinned versions.
- Clearly communicate limitations, including DRM-protected content and lack of iPhone remote control.

## Proposed technical direction

Investigate [UxPlay](https://github.com/FDH2/UxPlay) as an AirPlay protocol foundation before choosing an integration strategy. Start with a validated upstream receiver, then design a native Windows UI and a reproducible build and packaging workflow. UxPlay uses GPL-3.0; comply with its obligations before distributing derivatives.

## Roadmap

1. Validate the iPhone-to-Windows mirroring path on Windows 11.
2. Implement secure pairing and narrow firewall rules.
3. Add stable video/audio playback and latency diagnostics.
4. Build a minimal, accessible native Windows interface.
5. Automate builds and publish verifiable checksums, then test packaging.

See [architecture](docs/ARCHITECTURE.md), [security](docs/SECURITY.md), and [development plan](docs/ROADMAP.md).

## Development environment

- Windows 11, 64-bit
- Git
- Compiler and runtime requirements to be chosen after evaluating the receiver backend

## License

Not selected for this initial planning scaffold. If we incorporate or distribute UxPlay-derived code, we must follow its GPL-3.0 license and third-party notices.
