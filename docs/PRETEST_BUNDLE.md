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
