# Security policy and design goals

> **SECURITY HOLD (2026-11-05)**: UxPlay v1.73.7, used by the original build instructions, contains a reported heap buffer overflow (FDH2/UxPlay#614). Launchers are **disabled** pending review of a patched release. See [full review](../../docs/SECURITY_REVIEW.md).

The project now contains a Windows prototype controller and PowerShell launchers. **None of the code, network behavior, or media path has undergone a complete security audit or physical iPhone/Windows test.**

## Current prototype controls

- UxPlay v1.73.7 remains in the old instructions solely for traceability; it is **not an approved runtime**. The GUI and PowerShell entry points refuse to launch.
- `-pw` with no password is passed for a new random pairing PIN every connection.
- A separate visible UxPlay console displays the PIN; the .NET controller does not persist it.
- Launch is blocked if the physical connected Windows network profiles are not all Private.
- The controller does not add firewall rules or request administrator permission.

## Remaining risks and unimplemented controls

- Untrusted devices on the same LAN, Windows Public-profile exposure from **existing** firewall rules, and virtual network adapters.
- Malformed AirPlay packets, vulnerable media decoders, and unreviewed dependency binaries.
- Console PIN exposure during screen sharing; unexpected UxPlay log behavior.
- Key-management and bundled-distribution compliance are not independently assessed.
- Stopping from the GUI forcibly terminates the UxPlay process.

**Network profile checks do not ensure network confinement.** Review Windows Firewall rules separately and only test with non-sensitive content on a network you trust.

## Before public release

- Test the real protocol and network exposure; document observed results.
- Design a narrowly scoped firewall rule for Private networks and its uninstall path.
- Audit libraries and monitor vulnerabilities; generate verifiable release checksums.
- Review GPL-3.0 obligations, dependency notices and signing requirements.
- Establish a private process for reporting vulnerabilities.

Do not market the application as secure based solely on source availability.
