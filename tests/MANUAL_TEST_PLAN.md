# Manual acceptance tests (Windows 11 + iPhone)

Record the Windows version, iOS version, UxPlay commit SHA and date when running these tests. **No real-device tests have been run from this repository yet.**

| ID | Scenario | Expected |
| --- | --- | --- |
| 01 | Run prerequisite script before building UxPlay | Reports missing executable; does not install anything |
| 02 | Start from a connected Public Wi-Fi profile | Fails closed, does not launch UxPlay |
| 03 | Start with no connected physical network profile | Fails closed |
| 04 | Start with at least one Private and one Public physical adapter | Fails closed |
| 05 | Start from a trusted Private Wi-Fi/LAN with correct UxPlay path | Receiver starts; no administrator prompt from controller |
| 06 | Select `iPhone Mirror` in iPhone Screen Mirroring | Receiver appears via discovery and shows PIN in console |
| 07 | Connect with valid PIN, then disconnect and reconnect | New four-digit PIN is required each time |
| 08 | Enter incorrect PIN | Access is rejected |
| 09 | Rotate the iPhone, resize UxPlay window | Video remains usable in portrait and landscape |
| 10 | Play audio while mirroring | Sound plays on selected Windows audio device |
| 11 | Click Stop or press Ctrl+C in terminal | Receiver exits; iPhone is no longer mirroring |
| 12 | Start without required GStreamer plugins | Clear failure; no hidden automatic download |
| 13 | Check Windows Firewall after startup | No rule created by this project; Public access not allowed |
| 14 | Inspect repository and process arguments | No passwords, private keys, video recordings or telemetry written by controller |
| 15 | Build via Windows CI | .NET controller compiles and PowerShell files parse |

## Reproduction notes

- Test on **trusted local networks only**. Setting a network to Private merely for testing is not an acceptable mitigation on unknown networks.
- Preserve *non-sensitive* failure details only. Do not attach pairing PINs, screen captures containing private data, keys, or plaintext credentials to public GitHub issues.
- Explicitly mark a test **PASS**, **FAIL** or **NOT RUN**; do not assume success because source compiled.
