# Offline regression for UxPlay short-frame decryption

> **Experimental security research only. Not a production-approved patch.**

CI downloads pinned UxPlay v1.73.7 source, verifies the exact release commit, applies our proposed patch and compiles the **real upstream `lib/mirror_buffer.c`** in a C harness with AddressSanitizer and UndefinedBehaviorSanitizer. AES/SHA are deterministic test doubles. The tests exercise repeated short fragments, zero-length frames, 16-byte boundaries, mixed lengths and re-key state.

The test does not validate real OpenSSL cryptography, full AirPlay protocol handling, Windows behavior or network exposure. Passing it cannot authorize enabling the receiver.

Run on Linux with Git, a C compiler and sanitizer support:

```bash
bash scripts/ci/test-uxplay-mirror-sanitizers.sh
```

The patch is derived from upstream LGPL-2.1-or-later code in [UxPlay `mirror_buffer.c`](https://github.com/FDH2/UxPlay/blob/v1.73.7/lib/mirror_buffer.c). Preserve original notices.

Upstream reference: [issue #614](https://github.com/FDH2/UxPlay/issues/614); [upstream development fix](https://github.com/FDH2/UxPlay/commit/b1be2bd26629f822fa1f6ebbdb0c3a2ed30aa62f).

**Production launch remains disabled** by `BackendSecurityGate` and the PowerShell hold.

## Windows native functional tests

The same pinned UxPlay source and experimental patch are also compiled in MSYS2 UCRT64 on the Windows GitHub Actions runner, with deterministic and real OpenSSL cryptography tests. Run `bash scripts/ci/test-uxplay-mirror-windows.sh` inside an appropriately configured UCRT64 environment. **Windows runs do not use ASan/UBSan**; Linux runs provide the sanitizer coverage. Neither test starts an AirPlay network listener.
