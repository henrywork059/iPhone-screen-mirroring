# Windows 11 setup — first prototype (SECURITY HOLD)

> **DO NOT FOLLOW THESE BUILD/LAUNCH STEPS YET.** UxPlay v1.73.7 is affected by a reported heap buffer overflow ([FDH2/UxPlay#614](https://github.com/FDH2/UxPlay/issues/614)). The release pin below is historical and **not approved for execution**. Both project launchers now refuse to run. See [SECURITY_REVIEW.md](SECURITY_REVIEW.md). These instructions will be revised only after a reviewed patched release is selected.

This guide is for an initial **source-code prototype**, not an installer. You will need Windows 11 64-bit, a connected iPhone, a trusted local network, administrator permission for *installing development dependencies*, and a non-elevated account to run the receiver.

## 1. Install required tools

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for the Windows controller; verify with `dotnet --version`.
- [MSYS2](https://www.msys2.org/) for building UxPlay. Keep its default directory `C:\msys64`.
- **Bonjour SDK for Windows v3.0** (build dependency for **UxPlay v1.73.7**). Prefer Apple's official [developer downloads](https://developer.apple.com/download/all/?q=Bonjour%20SDK%20for%20Windows). The expected SDK location is `C:\Program Files\Bonjour SDK`. It may require an Apple developer login. Do not download repackaged binaries from unknown sites.

Open an **MSYS2 MSYS** terminal and update: 

~~~sh
pacman -Syu
~~~

Close/reopen the terminal if instructed. Open **MSYS2 UCRT64**, then install:

~~~sh
pacman -S --needed git mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-cmake mingw-w64-ucrt-x86_64-ninja mingw-w64-ucrt-x86_64-libplist mingw-w64-ucrt-x86_64-gstreamer mingw-w64-ucrt-x86_64-gst-plugins-base mingw-w64-ucrt-x86_64-gst-plugins-good mingw-w64-ucrt-x86_64-gst-plugins-bad mingw-w64-ucrt-x86_64-gst-libav
~~~

These package names and the Bonjour requirement follow [UxPlay's Windows build instructions for v1.73.7](https://github.com/FDH2/UxPlay/blob/v1.73.7/README.md#building-uxplay-on-microsoft-windows-using-msys2-with-the-mingw-64-compiler).

## 2. Historic vulnerable pin — DO NOT BUILD OR RUN

Still in **MSYS2 UCRT64**:

~~~sh
git clone https://github.com/FDH2/UxPlay.git
cd UxPlay
git checkout df67c212a433cf6dda3676dd40c097900d24e645
git rev-parse HEAD
~~~

The final command would report `df67c212a433cf6dda3676dd40c097900d24e645`, the **now-blocked** v1.73.7 release commit (September 4, 2026). Do **not** run this revision. We retain the old steps for traceability.

Build and install:

~~~sh
cmake -S . -B build -G Ninja
cmake --build build
cmake --install build --prefix /ucrt64
~~~

Expected executable: `C:\msys64\ucrt64\bin\uxplay.exe`.

**Build failure?** Confirm you opened **UCRT64**, installed Bonjour SDK, and see both CMake and GStreamer packages. This repository does not download or modify upstream dependencies automatically.

## 3. Preflight in Windows PowerShell

In the root of **this** project checkout (not the UxPlay checkout):

~~~powershell
powershell -NoProfile -File .\scripts\Check-Prerequisites.ps1
~~~

If the network is Public, use **Windows Settings → Network & internet → Wi-Fi/Ethernet → [connected network] → Network profile type → Private** only if this is a network you actually trust. Do **not** mark a café, hotel, or guest network Private merely to bypass the check.

## 4. Start the receiver — DISABLED UNTIL SECURITY REVIEW

Choose one route.

**Native controller (recommended after UxPlay is compiled):**

~~~powershell
dotnet run --project .\src\MirrorControl\MirrorControl.csproj
~~~

Click **Start receiver** and watch the separate **UxPlay console** for a 4-digit PIN.

**Terminal fallback (no .NET GUI needed):**

~~~powershell
powershell -NoProfile -File .\scripts\Start-Mirroring.ps1
~~~

To use a custom executable location:

~~~powershell
powershell -NoProfile -File .\scripts\Start-Mirroring.ps1 -UxPlayPath 'D:\Tools\uxplay.exe'
~~~

The launchers pass `-rc` with a known empty config file, `-n iPhone Mirror`, `-nh`, `-vs d3d11videosink`, `-as wasapisink`, and `-pw` with **no password argument**. In v1.73.7, `-pw` with no password requests a *new random PIN for every connection*. We deliberately do not use reusable `-pin` registration or store a plaintext password.

## 5. Connect the iPhone

1. Both devices must be on the same trusted LAN and be able to discover each other using mDNS/Bonjour; guest isolation and some VPNs can prevent discovery.
2. If Windows Firewall prompts about `uxplay.exe`, allow **Private networks only**. The project does not add rules or request elevation.
3. On the iPhone, open **Control Center → Screen Mirroring → iPhone Mirror**, then enter the random PIN shown in the UxPlay console.
4. Test portrait and landscape mirroring, sound playback, disconnection, and reconnecting. The PIN should change on each *new* connection.

If discovery fails, ensure Bonjour is working, the Windows firewall permits the specific application on the Private profile, and both devices are on the same subnet.

**Security limitation:** A Windows Private network label is not proof of trust. No launcher can guarantee that a separate service or pre-existing firewall rule does not expose UxPlay elsewhere. Check rules manually and do not mirror confidential content during initial tests.

## 6. Prototype limits and troubleshooting

- The GUI **does not embed the video or PIN**; UxPlay opens its own GStreamer video window and terminal.
- The GUI's Stop action forcibly terminates UxPlay and is not yet a graceful disconnect.
- If `d3d11videosink` or `wasapisink` is missing, install the GStreamer plugins listed above and inspect terminal output. The source intentionally does not silently fall back to insecure settings.
- If PowerShell reports a script policy restriction, run scripts from your own Git checkout with an appropriately configured execution policy. Do not disable Windows Defender or bypass SmartScreen for unverified downloads.
- Built artifacts remain on your machine. No executable, DLL, code signing certificate, pairing secret, or release file is included in this GitHub repository.

See [the manual test plan](../tests/MANUAL_TEST_PLAN.md) for exact pass/fail criteria.
