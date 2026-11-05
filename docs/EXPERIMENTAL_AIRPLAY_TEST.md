# Experimental AirPlay mirroring test (opt-in, not an approved release)

Unlike the offline GUI preview, this workflow **compiles, bundles, and enables a real AirPlay receiver** for a controlled Windows 11 + iPhone test.

## Build results

The first experimental job passed on [November 5, 2026](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42001238384) and uploaded the `iPhoneMirror-Experimental-AirPlay-MIRROR-ONLY` artifact. A subsequent build additionally checks that the experimental compile-time switch is truly enabled.

## Current secure build policy

The [AirPlay native security workflow](../.github/workflows/experimental-airplay-beta.yml)
compiles an unpublished candidate, runs exact-source patch checks,
native dependency verification, the offline loader check, and a native
SBOM generation step. It does **not** upload Windows executable artifacts.
A successful CI run is not an independent security audit, and the untrusted
native listener is not yet approved for device testing.

See [Security hardening status](SECURITY_HARDENING_STATUS.md).

## What it actually does

- Uses pinned UxPlay 1.74 **experimental** commit `7aaad6222c2e136723e6e3d25c3be784260c577d`, plus the local [short-frame fix](../patches/uxplay-experimental-mirror-short-frame.patch).
- Builds the complete UxPlay `.exe`, stages its native DLLs and GStreamer plugins, checks the four required media elements, and runs a help-only native load test.
- Builds a WinForms controller with the opt-in `EXPERIMENTAL_RECEIVER` switch. **Normal/default builds remain blocked.**
- Embeds the SHA-256 of the exact bundled UxPlay executable in the controller at build time and refuses to launch any other file.
- Asks for explicit confirmation **at every start**, validates Windows Private connection profiles, uses upstream `-pw` without password to show a new random 4-digit PIN each connection.
- Uploads a short-lived TEST artifact containing the GUI, patched backend, GStreamer runtime files and upstream patched corresponding source with license notices.

## Higher-quality mirroring profiles (new)

The Windows controller now offers **Compatible 1080p/30**, **Smooth 1080p/60**, **Sharper 1440p/60** (default), and **Ultra 4K/30**. The last two request UxPlay HEVC support (`-h265`) and require matching GStreamer H.265 parser/decoder plugins, which CI checks. A requested resolution/frame rate is advisory; some iPhones transmit less than requested. If a high-quality option fails, select a **1080p** profile and reconnect.

## Mirroring-only and Smart App Control

This version includes **no Bluetooth mouse/keyboard input**, ESP32 bridge,
keyboard capture or other remote-control features. It retains AirPlay
video/audio mirroring and all requested quality presets.

**Smart App Control remains ON.** Our unsigned experimental package may
still be blocked. This is expected for unsigned/untrusted software.
Do not turn off Smart App Control or bypass Windows Security warnings.
A future trusted signing certificate and security review are needed
before we can offer a compatible release.

## Limitations and risk

**This is not security-audited software and not recommended for normal use.** The experimental UxPlay AirPlay protocol and mDNS parsing surface remains unaudited. User confirmation and the Private network profile do **not** confine the process. Existing firewall rules may expose a network listener beyond the intended iPhone. Avoid sensitive content and untrusted networks.

UxPlay opens video in a separate GStreamer window. The PIN is printed in a separate console. No iPhone control, Bluetooth peripheral, embedded video canvas, signed installer, formal dependency audit or DRM video support is included.

## Security and installation guidance

Do not use historical unsigned GitHub Actions ZIPs, install the prototype,
disable Windows Smart App Control, disable Defender, add security exclusions,
or broaden firewall permissions to make an experimental receiver run.
The repository currently supports **source review and offline tests**.
Release requires an independent security audit, proper signed binaries,
a controlled firewall review and validated iPhone compatibility.

## Licensing and source

The test archive includes UxPlay GPL-3.0 license text and **the complete patched corresponding UxPlay source snapshot** under `native/source-notices/`. It also includes MSYS2 third-party license notices and an installed package inventory. See [dependency inventory](DEPENDENCY_INVENTORY.md); further distribution/legal review is still required before a public stable release.
