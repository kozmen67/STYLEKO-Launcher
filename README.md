# STYLEKO Launcher

Open-source reconstruction of the STYLEKO Knight Online launcher, prepared for reproducible Windows builds and later Authenticode signing.

## Safety rule

The current production `Launcher.exe` is not modified by this project. During validation the output name is intentionally `Launcher_TEST.exe`.

Known production binary reference:

`SHA256 30D80A32798596B062375E9FCBE368779026B9F8FB26656AEC45C5F5DA393877`

## Reconstructed production behavior

- Windows x86 executable.
- `requireAdministrator` manifest.
- 1120x585 borderless WebView2 launcher window.
- Uses `StyleKO\LauncherWeb\index.html` as the launcher UI.
- Handles WebView messages: `launcher-ready`, `drag`, `home`, `close`, `option`, `start`.
- Reads `Server.ini` (`Server/IP0`, `Version/Files`).
- Connects to launcher service on TCP port 15100.
- Implements the Knight Online launcher frame format `AA 55 + uint16 length + payload + 55 AA`.
- Requests remote version, notices and patch list.
- Downloads patch archives by FTP and extracts them into the client directory.
- Updates `Server.ini` `Version/Files` from numeric patch filenames.
- Rebuilds `object`, `item`, `fx`, `ui` HDR/SRC containers according to `Path.ini`.
- Runs `Option.exe` from the Option button.
- Opens `https://www.stylekopvp.com/` from Home.
- Displays `StyleKO\Guard\preview.html` in a 512x300 WebView2 guard window.
- After `guard-complete`, launches `KnightOnLine.exe` with the launcher process ID as its command-line argument.
- Keeps the launcher process alive for 5 seconds after a successful game launch before exiting.

## Build

Requirements:

- Windows 10/11
- Visual Studio 2022 with `.NET desktop development`
- .NET Framework 4.8 developer pack
- Internet access for NuGet restore

Open `STYLEKO.Launcher.sln`, select `Release | x86`, then Build Solution.

Normal Release output:

`STYLEKO.Launcher\bin\x86\Release\net48\Launcher.exe`

The validation GitHub Actions workflow overrides the assembly name to `Launcher_TEST.exe` so it cannot accidentally replace a production launcher during compatibility testing.

The project pins Microsoft WebView2 SDK `1.0.4258.31`.

## First test

Do not replace the production launcher.

1. Copy the entire production STYLEKO client directory to a test directory, for example `STYLEKO_TEST`.
2. Put the complete Release build output in the root of `STYLEKO_TEST`.
3. Confirm these existing client files are present beside it: `Server.ini`, `Path.ini`, `KnightOnLine.exe`, `Option.exe`, `StyleKO\LauncherWeb`, `StyleKO\Guard`.
4. Run `Launcher_TEST.exe` as administrator.
5. First validate UI, patch/version status and Option.
6. Then validate Start/Guard/game launch.
7. Do not rename or replace the production `Launcher.exe` until all checks pass.

## Release artifact

The release workflow builds `Launcher.exe` from the public repository on a GitHub-hosted Windows runner. The dedicated signing-input artifact contains only `Launcher.exe`; third-party runtime DLLs are not submitted as STYLEKO binaries for signing.

The launcher updates files only inside the STYLEKO game installation directory as part of its normal patching function. It does not install a Windows service or intentionally change operating-system security settings.

## Signing target

The intended next stage is SignPath Foundation. The repository should remain public and every signed binary should be produced by the CI workflow from the repository source. Do not upload the old closed-source `Launcher.exe` as a signing input.


## Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

See [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md) for build-origin, team-role, approval, and artifact rules.

See [PRIVACY.md](PRIVACY.md) for the launcher's network and privacy behavior.
