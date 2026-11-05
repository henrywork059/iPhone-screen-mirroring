/*
 * Experimental ESP32 USB-serial-to-BLE HID input bridge.
 * Requires original ESP32 with BLE and the pinned ESP32-BLE-Combo library.
 * Not a touch-screen injection method; iPhone must enable AssistiveTouch.
 * USB host is trusted: physical access to the serial port permits HID input.
 */
#include <Arduino.h>
#include <BleCombo.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

static char line[80];
static uint8_t used = 0;
static bool overflowed = false;
static bool buttonDown = false;

static bool signedValue(const char *value, long minValue, long maxValue, long *parsed, const char **after) {
  if (!value || *value == '\0') return false;
  char *tail = NULL;
  long n = strtol(value, &tail, 10);
  if (value == tail || n < minValue || n > maxValue) return false;
  if (parsed) *parsed = n;
  if (after) *after = tail;
  return true;
}

static void execute(const char *cmd) {
  // Never issue HID events without an authenticated BLE connection.
  if (!Keyboard.isConnected()) {
    Serial.println("NOT_PAIRED");
    return;
  }

  if (strcmp(cmd, "D") == 0) {
    if (!buttonDown) { Mouse.press(MOUSE_LEFT); buttonDown = true; }
    return;
  }
  if (strcmp(cmd, "U") == 0) {
    Mouse.release(MOUSE_LEFT);
    buttonDown = false;
    return;
  }

  if (strncmp(cmd, "M ", 2) == 0) {
    long dx = 0, dy = 0;
    const char *endX = NULL;
    const char *endY = NULL;
    if (signedValue(cmd + 2, -127, 127, &dx, &endX) &&
        endX && *endX == ' ' &&
        signedValue(endX + 1, -127, 127, &dy, &endY) &&
        endY && *endY == '\0') {
      Mouse.move((signed char) dx, (signed char) dy);
    }
    return;
  }
  if (strncmp(cmd, "W ", 2) == 0) {
    long wheel = 0;
    const char *end = NULL;
    if (signedValue(cmd + 2, -10, 10, &wheel, &end) && end && *end == '\0')
      Mouse.move(0, 0, (signed char) wheel);
    return;
  }
  if (strncmp(cmd, "T ", 2) == 0) {
    const char *value = cmd + 2;
    size_t n = strlen(value);
    if (n > 0 && n <= 48) {
      for (size_t i = 0; i < n; ++i) {
        if ((unsigned char)value[i] < 32 || (unsigned char)value[i] > 126) return;
      }
      Keyboard.print(value);
    }
    return;
  }
  if (strncmp(cmd, "K ", 2) == 0) {
    const char *key = cmd + 2;
    if      (strcmp(key, "ENTER") == 0)     Keyboard.write(KEY_RETURN);
    else if (strcmp(key, "BACKSPACE") == 0) Keyboard.write(KEY_BACKSPACE);
    else if (strcmp(key, "ESC") == 0)       Keyboard.write(KEY_ESC);
    else if (strcmp(key, "UP") == 0)        Keyboard.write(KEY_UP_ARROW);
    else if (strcmp(key, "DOWN") == 0)      Keyboard.write(KEY_DOWN_ARROW);
    else if (strcmp(key, "LEFT") == 0)      Keyboard.write(KEY_LEFT_ARROW);
    else if (strcmp(key, "RIGHT") == 0)     Keyboard.write(KEY_RIGHT_ARROW);
    return;
  }
}

void setup() {
  Serial.begin(115200);
  Keyboard.begin();
  Mouse.begin();
  Serial.println("IPHONE_HID_BRIDGE_READY");
}

void loop() {
  // UART input uses fixed memory; discard overlength frames until newline.
  while (Serial.available() > 0) {
    int b = Serial.read();
    if (b < 0) break;
    if (b == '\r') continue;
    if (b == '\n') {
      if (!overflowed && used > 0) {
        line[used] = '\0';
        execute(line);
      }
      used = 0;
      overflowed = false;
    } else if (!overflowed && used < sizeof(line) - 1) {
      line[used++] = (char)b;
    } else {
      overflowed = true;
    }
  }

  // Ensure a released mouse button after BLE disconnect/reconnect.
  if (!Keyboard.isConnected()) buttonDown = false;
  delay(1);
}
