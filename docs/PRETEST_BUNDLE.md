# Portable Windows bundle — package inspection only

This CI path verifies that a patched **experimental** UxPlay receiver, its runtime DLL dependencies, GStreamer plugins, and a self-contained Windows GUI can be collected into one Windows ZIP with a file-hash inventory.

Workflow: [Portable test-bundle packaging inspection](../.github/workflows/pretest-bundle.yml).

**It is not a downloadable installer.** The candidate ZIP is generated and deleted inside an ephemeral GitHub Actions runner; it is not uploaded, released, executed, or recommended for use. The application launch button remains blocked.

## Included during offline inspection

- Self-contained .NET 8 WinForms controller and empty `config/uxplayrc`.
- Pinned experimental UxPlay executable, corrected short-frame patch and upstream GPL-3.0 license notice.
- Directly and transitively imported MSYS2 UCRT64 runtime DLLs that can be resolved from the build environment.
- Installed GStreamer plugin DLLs and plugin-scanner executable if present.
- SHA-256 manifest for staged native files and explicit **DO NOT RUN** warnings.

## Not yet verified

1. Correct runtime playback and plugin discovery when relocated to a machine without MSYS2.
2. Complete third-party redistribution licenses, source offers, package vulnerability assessment and release signing.
3. The AirPlay pre-authentication attack surface and experimental internal mDNS implementation.
4. Real Windows firewall isolation and iPhone discovery, pairing, video and audio.

Packaging checks do not justify changing the receiver security hold. No test package should be distributed until security review and explicit validation approval.

## Completed inspection (2026-11-05)

[CI run #41976855941](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41976855941) passed. It assembled 241 GStreamer plugins and 208 native DLLs, generated a SHA-256 file inventory and inspected a 165,935,476-byte ZIP (about 166 MB). ZIP SHA-256 was `189486DF339EBEC77D5767A5D49AB987F2B8D42D5D0BA16030B70F7E83B219A8`, **informational for an unpublished and discarded build**.

The inspection discovered a missing optional JSON-GLib dependency, which was included for the successful rerun. The package was not distributed. An additional **help-only process loader check** is now configured: it invokes the pinned UxPlay binary with `-rc <empty config> -h` and checks for an immediate exit **before the receiver creates sockets**. This checks relocated DLL loading, not mirroring, pairing or network safety. No device or security certification is implied.
