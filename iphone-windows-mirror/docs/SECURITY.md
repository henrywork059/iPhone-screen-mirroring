# Security policy and design goals

This is an unimplemented project scaffold; it has **not** been audited.

## Threat model

- Untrusted devices on the same network attempting to connect.
- Malformed AirPlay packets and decoder input.
- Unintended exposure on public networks.
- Compromised build dependencies or release artifacts.
- Leakage of private screen content or pairing keys.

## Requirements before first public build

- Default to PIN pairing and reject unauthenticated sessions where protocol permits.
- Never enable a firewall exception for Public profiles by default.
- Keep traffic local; do not implement telemetry or remote relay.
- Avoid administrative privileges for routine application use.
- Minimize sensitive logs and ensure pairing secrets are stored safely.
- Pin and update dependencies; test malformed and unexpected network input.
- Generate SHA-256 checksums and document release provenance.
- Add instructions for reporting vulnerabilities privately before enabling public release.

## Handling uncertain security claims

Do not market the software as 'secure' solely because it is open source. Require security testing and review before making security guarantees.
