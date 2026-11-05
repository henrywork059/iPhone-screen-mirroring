# Architecture (proposal; not yet implemented)

## Data flow

1. iPhone broadcasts and discovers compatible AirPlay receivers over a trusted local network.
2. The receiver accepts a user-authorized, authenticated session.
3. The protocol layer handles AirPlay control and receives compressed video/audio.
4. The playback layer decodes and renders frames and audio on Windows.
5. The UI exposes connection state, fullscreen/resize, PIN controls, and diagnostics.

## Potential components

- **Protocol backend:** evaluate maintained UxPlay releases, upstream fixes, and integration choices.
- **Video:** prefer hardware acceleration where viable; provide a software fallback.
- **Audio:** synchronized local playback on the selected output device.
- **UI:** native Windows 11 interface, initially a simple controller for the backend.
- **Security:** pairing required by default, trusted networks only, least-privilege installation.
- **Packaging:** reproducible pipeline, pinned dependency versions, release checksums, version information.

## Decisions still open

- Native UI framework: WinUI 3, Qt, or another suitable Windows stack.
- Backend interface: separate process vs integrated library.
- Whether the project can avoid elevated permissions entirely.
- Supported codecs and exact minimum iOS version.
- Repository licensing after the integration approach is chosen.
