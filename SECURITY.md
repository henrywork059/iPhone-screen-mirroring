# Security policy

## Supported versions

**No version is currently approved for installation or use.** This is an unapproved mirroring-only prototype. Normal receiver launch stays disabled; security CI builds unpublished experimental candidates. Signed distribution and independent review remain pending.

See [the security review](docs/SECURITY_REVIEW.md) and [security issue #2](https://github.com/henrywork059/iPhone-screen-mirroring/issues/2).

## Reporting vulnerabilities

Please **do not disclose exploit details, credentials, pairing PINs, crash dumps containing private information, or unpublished security vulnerabilities in public GitHub issues**.

If the repository's **Private vulnerability reporting** option is enabled, use its GitHub Security tab to report privately. Otherwise, reach the repository maintainer through a private channel before sending sensitive details. Do not assume a public issue is private.

## Development safeguards

- Never turn off `BackendSecurityGate` or the PowerShell security hold merely because a compile or regression test passes.
- Do not distribute compiled UxPlay executables or native DLLs from unreviewed sources.
- Pin upstream dependency commits and verify the exact source before applying patches.
- Test on isolated networks first, with non-sensitive sample content only after explicit review.
- Review the application's firewall exposure, cryptographic dependencies, codec parser behavior, and license obligations before any release.

A successful CI job verifies only the checks listed in that job. It is **not** a security certification.

## Security hardening (November 2026)

[Current source hardening, test evidence and outstanding release gates](docs/SECURITY_HARDENING_STATUS.md).

No new unsigned executable artifacts are uploaded. Do not disable Smart App Control or bypass Windows Security warnings to run previous test ZIPs.
