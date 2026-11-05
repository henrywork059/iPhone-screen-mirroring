# ESP32 Bluetooth mouse/keyboard bridge (experimental)

## What it is

The Windows **Input pad (ESP32)** window sends newline-framed commands over **USB serial** at **115200 baud**. This firmware runs on an **original ESP32 with BLE support** and presents itself to the iPhone as a paired Bluetooth mouse and keyboard. It is **not** an AirPlay input channel and cannot inject real multi-finger touchscreen events.

The iPhone must enable **Settings → Accessibility → Touch → AssistiveTouch** and pair with the device under Bluetooth / AssistiveTouch pointer devices. Bluetooth HID cursor behavior varies by iOS version and accessibility settings. A Windows laptop's own Bluetooth adapter is **not** a replacement for this external bridge.

## Dependencies (not bundled)

- Arduino IDE with Espressif Arduino ESP32 core **2.0.17** (initial compile target).
- [blackketter/ESP32-BLE-Combo](https://github.com/blackketter/ESP32-BLE-Combo) library at pinned commit `b25c1cb9dbff64374d2fd4ad307a184506eb9232`, MIT-licensed.
- An ESP32 DevKit-class board with BLE and a working USB serial port. **ESP32-S2 lacks Bluetooth and cannot be used.**

The firmware and library compilation are independently checked in GitHub Actions when firmware changes are made; **Bluetooth pairing to a physical iPhone is still untested**.

## Steps when you have compatible hardware

1. Install the above exact core and library in Arduino IDE; open `firmware/esp32-hid-bridge/esp32-hid-bridge.ino`.
2. Select your ESP32 board and flash over USB. Disconnect any other serial monitor that has the COM port open.
3. Pair the new BLE HID device with your iPhone. Turn on AssistiveTouch so the pointer can act on the interface.
4. In the experimental Windows mirror app, click **Input pad (ESP32)**, choose the device's COM port and **Connect USB**.
5. Move the mouse across the rectangular pad to move the iPhone's AssistiveTouch pointer, left-click to tap, click-drag to drag, scroll with mouse wheel, and type printable ASCII text in the text field then press **Send text**.

**Important limitations:** The AirPlay video opens separately in UxPlay's GStreamer window. Our control pad cannot intercept clicks on that video window. It uses relative cursor movement, not absolute tap coordinates. The bridge does not support multitouch, pinch, full-screen keyboard mapping, authentication codes, or arbitrary Unicode text. Pairing and movement speeds may need calibration.

## Local serial command protocol

| Command | Meaning |
| --- | --- |
| `M dx dy\n` | Relative pointer move; dx/dy each -127..127 |
| `D\n` | Hold left mouse button |
| `U\n` | Release left mouse button |
| `W n\n` | Scroll n ticks, -10..10 |
| `T text\n` | Send 1–48 printable ASCII characters |
| `K ENTER\n`, `K BACKSPACE\n`, etc. | Supported special keys |

The firmware ignores inputs unless the BLE keyboard is connected. Invalid/oversized serial frames are ignored. There is no network listener, logging or remote update function in the bridge.

**Security:** The connected USB host is trusted and can send HID commands to a paired iPhone. Do not pair this device with someone else's phone. Disconnect USB when finished; unpair from Bluetooth when it is no longer required.
