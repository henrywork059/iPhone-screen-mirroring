# Windows standalone GUI package — offline only

This repository can now **build** a self-contained Windows 11 x64 GUI using .NET 8 Windows Forms without requiring the user to install the .NET SDK. The CI checks the presence of `MirrorControl.exe` and `config/uxplayrc` inside the output directory and creates a ZIP locally.

**Security hold:** The GUI's receiver Start button remains disabled. The ZIP intentionally **does not contain or launch UxPlay**. This is **not** a usable iPhone mirroring installer.

Workflow: [Windows offline controller packaging](../.github/workflows/controller-package.yml). It records a ZIP SHA-256 in the CI log and discards the ZIP. No executable is made available for download until package review.

Outstanding work before handoff: build a reviewed receiver runtime with its DLLs and GStreamer plugins, verify licensing, ensure an isolated firewall configuration, choose packaging, and conduct a controlled iPhone test.
