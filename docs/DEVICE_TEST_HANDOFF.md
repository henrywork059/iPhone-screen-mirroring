# Device-test handoff — suspended pending security approval

> **DO NOT INSTALL OR RUN HISTORICAL BETA ZIPS.** Smart App Control must remain
> enabled. Our experimental native receiver remains under security review,
> and no new unsigned executable artifacts are distributed.
> [Security hardening status](SECURITY_HARDENING_STATUS.md) is the authoritative
> current release policy. The steps below are **future test criteria**, not
> current authorization to run a network receiver.

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

## Current safe checks — source review and CI only

The offline Windows source checks, native C regression workflows,
libFuzzer tests and unpublished binary/package checks can run in CI.
No unsigned Windows executable should be downloaded or launched.
For physical tests after independent review and trusted publisher signing,
use the future checklist below.

## What will require you (later)

1. A Windows 11 laptop and iPhone, connected **only to an isolated trusted Wi-Fi or LAN** (preferably a temporary access point with no unrelated devices). Never use a hotel, café, office, or guest network that has other clients.
2. Before connecting, inspect Windows Firewall and confirm that no rule allows broad inbound access for the receiver. Restrict test access to the iPhone. If the iPhone's address changes, revise the rule rather than opening it to all devices.
3. Run the reviewed test build **as a normal user**; perform pairing with a fresh PIN and display non-sensitive content only.
4. Confirm video, audio, iPhone rotation, connection rejection with a bad PIN, disconnect and reconnect, and clean shutdown. Inspect actual Windows Firewall behavior.
5. Stop the receiver, remove temporary firewall rules, and record results with **no PINs, keys, recordings, or personal screen content** in public GitHub issues.

The old experimental beta instructions are historical. Do not run an unsigned mirror receiver; refer to [Security hardening status](SECURITY_HARDENING_STATUS.md).

## Read-only preflight

`scripts/Inspect-TestReadiness.ps1` checks connected profiles, executable hash and program-specific firewall rules without changing settings or launching a network service. It cannot establish comprehensive isolation on its own; broad port rules and router policies still require manual examination.
