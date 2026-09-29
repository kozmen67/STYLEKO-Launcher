# STYLEKO Launcher v1.0.0.2 — unsigned public release candidate

This is the first public open-source release of the STYLEKO Windows launcher component.

## Status

This release is intentionally **unsigned**. It is published before the SignPath Foundation application so the exact application form intended for future signing is publicly available and auditable.

## Binary

- File: `Launcher.exe`
- Architecture: Windows x86
- Product: STYLEKO Launcher
- Version: 1.0.0.2
- SHA-256: `0911033439035F636AE1D637E7E554AB14BED349DEC499B840158D3685B82DA6`
- Authenticode: unsigned

## Functionality

- Displays the STYLEKO WebView2 launcher UI.
- Checks launcher version and patch metadata.
- Downloads and applies game patches.
- Rebuilds configured HDR/SRC game resource containers after patching.
- Starts `Option.exe`.
- Runs the local STYLEKO Guard UI and then launches `KnightOnLine.exe`.

## Requirements

This launcher component is intended for an existing STYLEKO game installation. The proprietary game binaries and game assets are not part of this open-source repository or release.

Required adjacent game/client files include `Server.ini`, `Path.ini`, `KnightOnLine.exe`, `Option.exe`, and the local `StyleKO\LauncherWeb` / `StyleKO\Guard` UI assets.

## Code signing policy

See [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

Free code signing provided by SignPath.io, certificate by SignPath Foundation.
