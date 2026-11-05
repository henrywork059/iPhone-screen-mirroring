# Development roadmap

## Milestone 0 — feasibility
- [x] Identify UxPlay v1.73.7 stable release, Windows 11 build requirements, and GPL-3.0 license.
- [x] Pin its release commit in [Windows setup](../../docs/WINDOWS_SETUP.md).
- [ ] Reproduce Windows UxPlay build on a physical laptop.
- [ ] Validate iPhone discovery, video, audio, and PIN entry.

## Milestone 1 — initial receiver controller (source present, not device-verified)
- [x] Create a .NET 8 Windows Forms process controller.
- [x] Configure UxPlay to require a fresh random PIN for each connection.
- [x] Block launch if connected physical network profiles are not Private.
- [x] Provide a PowerShell terminal fallback.
- [ ] Verify no Public-profile firewall access in a Windows 11 test.
- [ ] Confirm UxPlay console displays PIN and mirroring works in practice.
- [ ] Replace process-tree termination with graceful shutdown.

## Milestone 2 — native UI
- [ ] Embed video and display PIN in our Windows window.
- [ ] Add connection status, fullscreen, always-on-top, and a disconnect action.
- [ ] Improve GStreamer setup validation and actionable diagnostics.

## Milestone 3 — security and distribution
- [ ] Audit dependency versions and known vulnerabilities.
- [ ] Check firewall exposure and authentication on real networks.
- [ ] Record build provenance, add release checksums, and assess code signing.
- [ ] Test installer/uninstaller and cleanup.
- [ ] Review GPL obligations and choose a license for original project code.

See [manual acceptance tests](../../tests/MANUAL_TEST_PLAN.md). Compiling source does **not** count as passing the actual device tests.
