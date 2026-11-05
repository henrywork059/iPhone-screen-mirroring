# Windows standalone GUI preview — safe, offline-only test

**Status: CI-built UI preview available.** This is **not** an iPhone screen mirroring installer.

Download from the successful [Windows GUI preview run](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41978061900) using the **Artifacts** section and select `iPhoneMirror-Offline-UI-Preview`. This artifact expires on **November 12, 2026**, unless rebuilt or retained separately.

The downloadable artifact ZIP contains a second archive named `offline-controller.zip`. Extract both archives before running `MirrorControl.exe`.

## What is inside

- Self-contained .NET 8 x64 Windows Forms app (`MirrorControl.exe`), so the .NET SDK is not required on your Windows 11 laptop.
- The `config/uxplayrc` file and an explanatory `READ-ME-FIRST.txt`.
- A deliberately **disabled Start Receiver button**, with a second hard stop in the process launcher.

**Not included:** UxPlay receiver, GStreamer binaries, AirPlay listener, screen mirroring capability, installer or firewall changes. The app should not create or open a network listener.

## What to check on your Windows laptop

1. Download the artifact from the GitHub Actions run, extract the nested `offline-controller.zip` to a new folder.
2. Examine `READ-ME-FIRST.txt`. Run `MirrorControl.exe` as a normal user.
3. Confirm the dark window opens, the status says **SECURITY HOLD**, and the **Launch blocked** button is disabled.
4. Close the window. Report whether it opened correctly, whether Windows security software reported a warning, or whether there were any errors.

If Windows Defender or SmartScreen warns about the unsigned executable, **do not disable protection or bypass the warning**. Report the exact warning; signing/provenance remains on the release roadmap.

**Do not attempt iPhone pairing, enable firewall access or install third-party UxPlay binaries for this preview.** For the future live device test, see [DEVICE_TEST_HANDOFF.md](DEVICE_TEST_HANDOFF.md).

The preview checks only Windows UI startup and layout. It does not establish AirPlay functionality, security or release readiness.
