# Windows Bluetooth HID input (no ESP32)

The experimental Windows mirroring controller can run a bundled **BleHid CLI v0.6.0**
Bluetooth LE keyboard/mouse companion. It uses your PC's built-in BLE peripheral
role, not AirPlay, to send normal external keyboard/mouse input to the iPhone.

**Hardware requirements:** Windows 11 Bluetooth LE adapter with the peripheral
role enabled, and an iPhone that accepts BLE HID keyboard/mouse devices. The
target laptop's diagnostic already reported `LE=True, Peripheral=True`,
successful HID GATT service initialization, and advertising `Started`.
**Actual iPhone pairing and pointer movement have not yet been confirmed.**

## Verified Windows test package (build + upload)

The experimental Windows build has passed:
[GitHub Actions #42066227443](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42066227443).
Under **Artifacts**, download `iPhoneMirror-Experimental-AirPlay-TEST`
and extract its inner ZIP. The inner ZIP is **204,998,910 bytes**,
SHA-256 `B39F7A198243951D45145CD4AEBDE82383FCC394FF520D4D260802590F775414`.
The artifact is retained until **November 12, 2026**.

This confirms native and managed CI builds, archive checksums, and packaging,
not successful pairing with a physical iPhone.

## Pairing and first test

1. Exit the earlier standalone **BleHid.Cli.exe** at its `>` prompt with
   `quit` (also fully quit its desktop/tray app, if installed). Only one
   Bluetooth HID provider should run on the PC.
2. Extract both ZIP layers from the updated **experimental mirroring build**
   and run `MirrorControl.exe` as a standard Windows user.
3. Click **Bluetooth control**. Click **Start Bluetooth** and confirm the
   experimental pairing prompt. The companion uses encrypted HID GATT reports;
   there is no `--plain` option and no background autostart.
4. On iPhone open **Settings → Bluetooth**, find the Windows PC's advertised
   HID device and pair with it. Also enable **Settings → Accessibility →
   Touch → AssistiveTouch** to show a mouse pointer.
5. In the Bluetooth control window click **Refresh paired hosts**. Select
   only **your iPhone** from the list and click **Use selected iPhone**.
   The controller verifies that the CLI selected that named device.
6. Test the **arrow**, **Tap / click**, **Scroll** and **Send text** commands,
   using only non-sensitive content.
7. For normal keyboard and mouse redirection click **Capture Windows mouse +
   keyboard** and confirm. While capturing, Windows local input is redirected
   to the selected iPhone. **Press Ctrl+Alt+Q to end capture and restore
   Windows input.** This recovery shortcut is implemented by upstream BleHid.
8. Click **Stop Bluetooth** and **Stop receiver** when finished.

**Important:** You still view the iPhone in a separate UxPlay/GStreamer video
window. Bluetooth HID movement is RELATIVE, not absolute touch coordinates.
Capturing mouse/keyboard does not automatically map each mirrored pixel to an
iPhone touch coordinate. If the pointer is displaced, move it on the iPhone,
or use the arrow buttons to calibrate before capture.

## Corrected build for paired-but-empty HID host list

The parser correction has passed offline Windows tests, and the corrected
experimental mirroring + Bluetooth package has also passed its full Windows
build: [GitHub Actions run #42070566283](https://github.com/henrywork059/iPhone-screen-mirroring/actions/runs/42070566283).
Under **Artifacts**, download `iPhoneMirror-Experimental-AirPlay-TEST`
and extract both ZIP layers. This artifact expires November 12, 2026.

Verified inner ZIP SHA-256:
`1B47A51EB7708096AC75CD35FD2C5271F22EB4916F171CC00249D746024E32D9`.

The corrected **Refresh paired hosts** button now queries `peers`,
`status`, and `host`, normalizes the CLI prompt, and reports
Bluetooth LE/Classic connection differences and missing HID subscriptions.
The package has been compiled and inspected, but its real iPhone pairing
behavior still needs device testing.

## Important fix: empty Refresh paired hosts list

Older test builds incorrectly parsed the CLI's `> ` prompt and could hide
subscribed iPhones from the dropdown. The corrected GUI strips that prompt,
then reads keyboard/mouse subscriber counts and selected-host confirmation.

**Refresh paired hosts** now also executes the upstream CLI `peers` command
and shows connected LE/Classic peer counts. A Bluetooth connection showing
"Connected" in iPhone Settings does not itself prove that the iPhone
subscribed to the BLE HID report characteristics.

If the list remains empty, inspect the GUI log for:
- `connected LE (N)` — BLE connection count
- `connected classic (N)` — ordinary Bluetooth connection count
- `keyboard subs : N`, `mouse subs : N` — real HID report subscribers
- `(no subscribed hosts)` — iPhone has not registered as a usable HID host

A Classic connection without an LE HID subscriber, or an LE connection with
zero report subscribers, may require forgetting the Windows Bluetooth device
on the iPhone and pairing again while Bluetooth advertising is already
running. Doing so removes the existing Bluetooth bond and may affect other
services sharing that connection.

## If the iPhone is paired but there is no input

Use **Refresh paired hosts** and inspect the log. The CLI must report both
`keyboard subs` and `mouse subs` as nonzero, and **Use selected iPhone**
must confirm the intended target before our app sends input.

If the earlier standalone BleHid CLI was open at the same time, type `quit`
there and stop/restart the new Bluetooth control window. If pairing persisted
but report subscriptions are missing after a restart, the BleHid upstream
project documents possible host GATT cache/reconnect limitations. You may
need to disconnect/reconnect Bluetooth or forget/re-pair your own PC in
iPhone Bluetooth settings. Do not pair to unrelated devices.

## No iPhone Developer Mode needed

Bluetooth HID pairing uses the standard iPhone external keyboard and
AssistiveTouch pointer features. This path does not require Xcode, a jailbreak,
a signed iOS app, USB developer pairing, or Developer Mode.

## Proven and unproven

- **Proven on the target PC:** Adapter reports `LE=True`,
  `Peripheral=True`, and the GATT HID keyboard/mouse services and BLE
  advertisements successfully started.
- **Documented by upstream BleHid:** Paired iPhone keyboard and mouse input
  works with AssistiveTouch, but iPhone reconnect-after-restart behavior is
  untested. This is third-party evidence, not our own physical test.
- **Our CI verifies:** Windows application builds, BLE command grammar,
  official GitHub v0.6.0 release ZIP SHA-256,
  checksum-binding of the bundled executable, and license inclusion.
- **Not yet verified:** Pairing/host selection with your exact iPhone,
  usable gesture latency, relative pointer calibration, security audit, and
  stability after sleep/restart.

## Source, version pin and licenses

BleHid is MIT licensed by Abhishek Raj. The beta bundles the upstream
v0.6.0 **win-x64 CLI**, checked against the GitHub release asset SHA-256:

`0BBB2E85BEBC783E304E3E46769F3000897F18B72FA646B253B3B0FF292F87B7`

- Source: https://github.com/abhishek-raj/windows-ble-hid
- Release: https://github.com/abhishek-raj/windows-ble-hid/releases/tag/v0.6.0
- Bundled license: `bluetooth/LICENSE-MIT.txt`

At CI publish time, the exact `bluetooth/BleHid.Cli.exe` SHA-256 is embedded
in the experimental GUI. The GUI refuses to launch a changed executable.
Normal/default builds remain blocked and do not bundle or launch the HID CLI.

**Security and privacy:** The optional companion is third-party executable
code and has not undergone an independent security audit. It advertises a
Bluetooth input device and sends commands only when you deliberately start
it and select the target iPhone. Nearby pairing requests should be declined
unless they are yours. Only type non-sensitive content during beta testing.
Never bypass Windows Defender or SmartScreen warnings.
