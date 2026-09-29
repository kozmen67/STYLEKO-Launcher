# Reconstruction notes

These facts were observed from the existing production binary without modifying it:

- SHA-256: `30D80A32798596B062375E9FCBE368779026B9F8FB26656AEC45C5F5DA393877`
- PE32 x86, unsigned.
- Product/description/company: STYLEKO Launcher / STYLEKO Launcher / STYLEKO.
- File/Product version: 1.0.0.1.
- Embedded manifest requests `requireAdministrator`.
- Debug paths: `C:\KnightOnline\Source\Launcher\Launcher.cpp`, `APISocket.cpp`, `APISocket.h`, `Win32\Release\Launcher.pdb`.
- WebView2 strings and launcher/guard HTML paths are present.
- Game launch trace string: `StartGameClient: exe="%s" cwd="%s" args="%s"`.
- Game is launched with `ShellExecuteA(..., SW_RESTORE)` and the launcher PID as arguments.
- After successful launch, timer lifetime is 5000 ms (`0x1388`).
- Guard window creation uses width 512 (`0x200`) and height 300 (`0x12c`).
- `Path.ini` reads `[Dir] count`, indexed directory names, and `[Version] Count`.
