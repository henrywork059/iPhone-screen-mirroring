# Experimental AirPlay mirroring test (opt-in, not an approved release)

Unlike the offline GUI preview, this workflow **compiles, bundles, and enables a real AirPlay receiver** for a controlled Windows 11 + iPhone test.

## Build results

The first experimental job passed on [November 5, 2026](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42001238384) and uploaded the `iPhoneMirror-Experimental-AirPlay-MIRROR-ONLY` artifact. A subsequent build additionally checks that the experimental compile-time switch is truly enabled.

## Verified mirroring-only build

Use the [experimental mirroring-only CI workflow](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/experimental-airplay-beta.yml)
to locate a successful build. Its job logs include the exact SHA-256 of the
**inner** ZIP, plus the hash pinned into the controller for the native UxPlay.
Never reuse a historical ZIP checksum for a newer build. These checks
verify reproducibility/integrity, **not** Smart App Control trust or
security approval.

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

## When a CI artifact is available

1. Open the successful [experimental AirPlay workflow](https://github.com/henrywork059/iPhone-screen-mirroring/actions/workflows/experimental-airplay-beta.yml).
2. Download `iPhoneMirror-Experimental-AirPlay-MIRROR-ONLY` from a successful run and extract the nested ZIP into a directory, **only if you accept the limitations**.
3. Read `READ-ME-FIRST-EXPERIMENTAL.txt`. Check that Windows Defender and SmartScreen do not raise a warning; **do not bypass warnings**.
4. Use a trusted, isolated Private-network Wi-Fi connection shared with your iPhone. Run `MirrorControl.exe` as a normal user.
5. Click **Start test receiver** and accept the explicit experimental test prompt. If Windows asks for firewall permission, choose **Private only**, never Public.
6. On your iPhone open Control Center → Screen Mirroring → `iPhone Mirror`, enter the PIN from the separate UxPlay console and check whether a GStreamer video window displays your screen.
7. Test only non-sensitive content. Stop the receiver and close it afterward. Do not post pairing PINs, keys, or screenshots of personal information.

Report Windows/iOS versions and whether the receiver appears, whether a PIN appears, whether video/audio play, and any sanitized error text. **A successful build is not proof mirroring works on your device**; that is what this supervised test establishes.

## Licensing and source

The test archive includes UxPlay GPL-3.0 license text and **the complete patched corresponding UxPlay source snapshot** under `native/source-notices/`. It also includes MSYS2 third-party license notices and an installed package inventory. See [dependency inventory](DEPENDENCY_INVENTORY.md); further distribution/legal review is still required before a public stable release.
