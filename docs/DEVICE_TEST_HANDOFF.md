# Device-test handoff — experimental mirroring test possible

> **Opt-in beta now available:** the separate [experimental AirPlay test build](EXPERIMENTAL_AIRPLAY_TEST.md) contains a patched UxPlay receiver and a controller that can start it. Real iPhone compatibility and full network security remain unverified. Normal builds remain disabled. Test only if you accept the documented risk and can use an isolated trusted network; the package is not for normal use.

## Remote development already completed

- Windows controller source and .NET source compilation.
- Linux ASan/UBSan tests of patched UxPlay short-frame decryption, including real OpenSSL.
- Windows UCRT64 native C tests.
- **Full Windows executable build** from pinned experimental UxPlay source at commit `7aaad6222c2e136723e6e3d25c3be784260c577d` with local short-frame correction.
- Dependency and DLL import information recorded in [DEPENDENCY_INVENTORY.md](DEPENDENCY_INVENTORY.md).

## Outstanding checks before a stable release

- Independent review of native AirPlay request parsers and the new experimental mDNS implementation, including pre-authentication paths.
- Verification that dynamic GStreamer plugins and transitive DLLs come from trusted packages; record a complete manifest.
- Verify that the eventual Windows receiver package contains exact pinned and reviewed components, licenses, and SHA-256 checksums.
- Implement and review a narrow inbound firewall policy limited to **Private** network profile and the iPhone's specific address; verify no broader existing rules.
- Approve a **test-only build** for a dedicated trusted network, not normal daily use.

## First safe device check — offline UI only

The [UI-only GitHub Actions preview](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/41978061900) can be inspected on your Windows laptop now. It includes **no** UxPlay, GStreamer or AirPlay service, and its launch controls are disabled. Download the `iPhoneMirror-Offline-UI-Preview` artifact, extract both nested ZIPs, read `READ-ME-FIRST.txt`, and launch `MirrorControl.exe` as a standard user. Verify the dark UI displays a visible **SECURITY HOLD** and the receiver Start control is disabled. Do **not** bypass Windows SmartScreen or antivirus alerts if shown; report them instead.

No iPhone or firewall changes are needed for this offline UI check. This must not be confused with a live mirroring test. [Detailed instructions](OFFLINE_CONTROLLER_PACKAGE.md).

## What will require you (later)

1. A Windows 11 laptop and iPhone, connected **only to an isolated trusted Wi-Fi or LAN** (preferably a temporary access point with no unrelated devices). Never use a hotel, café, office, or guest network that has other clients.
2. Before connecting, inspect Windows Firewall and confirm that no rule allows broad inbound access for the receiver. Restrict test access to the iPhone. If the iPhone's address changes, revise the rule rather than opening it to all devices.
3. Run the reviewed test build **as a normal user**; perform pairing with a fresh PIN and display non-sensitive content only.
4. Confirm video, audio, iPhone rotation, connection rejection with a bad PIN, disconnect and reconnect, and clean shutdown. Inspect actual Windows Firewall behavior.
5. Stop the receiver, remove temporary firewall rules, and record results with **no PINs, keys, recordings, or personal screen content** in public GitHub issues.

The [experimental beta guide](EXPERIMENTAL_AIRPLAY_TEST.md) provides the first testable mirror build. It does **not** bypass the stable-release hold. Do not use personal or confidential content.

## Read-only preflight

`scripts/Inspect-TestReadiness.ps1` checks connected profiles, executable hash and program-specific firewall rules without changing settings or launching a network service. It cannot establish comprehensive isolation on its own; broad port rules and router policies still require manual examination.
