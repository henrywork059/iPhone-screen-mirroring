# Windows executable: signed release procedure

**Current status: no distributable Windows EXE yet.** The project compiles
`MirrorControl.exe` on a Windows CI runner, but unsigned packages remain
blocked because the receiver has not passed independent review and testing.

The manual workflow [Signed Windows mirroring release](../.github/workflows/signed-windows-release.yml)
now creates a **signed, mirroring-only Windows ZIP** only after external
reviewers and a trusted publisher identity have been configured. Ordinary
push workflows do not upload executable files. Never disable Smart App Control,
Defender or Windows Firewall as part of this process.

## What must happen before downloading an executable

1. Obtain a documented **independent security review** of the actual UxPlay
   receiver, native media dependencies, the local patches, build and signing
   controls, and release candidate.
2. Complete the [device-test checklist](DEVICE_TEST_HANDOFF.md) on an
   isolated, owned Windows 11 PC and iPhone after independent test clearance,
   including firewall exposure, discovery, PIN handling, 1080p/HEVC media,
   reconnect and shutdown. This repository has **not** done those tests.
3. Obtain a legitimate, publicly trusted Authenticode code-signing identity
   for the Windows publisher, or replace the PFX signing adapter with an
   authorized Microsoft Artifact Signing/Store signing workflow.
   A self-signed certificate does not satisfy Smart App Control.
4. In GitHub repository **Settings > Environments**, create
   `approved-windows-release` and configure required independent reviewers
   and restricted allowed branches. Release must not be left unprotected.
5. Only **after review and real-device tests** set the following environment
   variables to the exact approved source revision and certificate subject:
   `SECURITY_REVIEWED_SHA`, `DEVICE_TESTED_SHA`, and
   `TRUSTED_PUBLISHER_SUBJECT`.
6. Add environment secrets `WINDOWS_SIGNING_PFX_BASE64` and
   `WINDOWS_SIGNING_PFX_PASSWORD` for an approved, trusted publisher PFX.
   Use the GitHub environment secret UI, **not chat, issues, source code,
   commits, or unprotected Action logs**. Keep key material in a managed
   signing solution when possible; the current workflow uses an ephemeral
   PFX to support the approved-certificate path.
7. In **Actions > Signed Windows mirroring release (review and signing
   required) > Run workflow**, select the approved `main` commit and
   enter a SemVer version such as `0.1.0`.
8. After required human environment approval, GitHub CI builds the pinned
   receiver, signs **every native EXE and DLL**, rebuilds the native hash
   manifest, compiles `MirrorControl.exe` bound to the signed native hashes,
   signs the GUI, validates every shipped executable signature and publisher,
   and uploads `iPhoneMirror-win-x64-signed` only on full success.
9. Download the artifact from that successful approved run, extract the
   artifact ZIP and then the inner signed release ZIP, retaining the complete
   `native/` and `config/` folder structure. **Do not move
   `MirrorControl.exe` out of the release folder by itself.**
10. Verify the trusted publisher in **Windows Properties > Digital
    Signatures**. Finally confirm Smart App Control behavior on a clean
    Windows 11 test machine before considering general distribution.

## Release controls

- The manual workflow requires both human-approval evidence variables to
  match the **exact checked-out Git commit**, and an expected trusted
  publisher subject.
- The signer requires approved PFX credentials, timestamping and valid
  Authenticode signatures on all packaged EXEs/DLLs. It never uploads an
  unsigned fallback ZIP.
- The native integrity manifest is recalculated **after code signing**,
  otherwise signing would invalidate the app's original SHA-256 checks.
- The `approved-windows-release` environment must have independent
  required reviewers; a repository secret and SHA field alone are not a
  substitute for audit evidence.
- No signed release has been built or offered in this change because the
  external review, physical device testing and signing identity remain
  unavailable. A signed Windows program also is not automatically safe:
  Windows reputation systems may still block a legitimate but unrecognized
  certificate, and a separate clean-machine test is required.

See [security hardening status](SECURITY_HARDENING_STATUS.md).
