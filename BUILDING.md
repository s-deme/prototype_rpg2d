# Building and release

## Local Windows build

1. Open the project with Unity 2022.3.62f1 or newer.
2. Run `./scripts/Test-ReleasePreflight.ps1` from PowerShell to check release inputs, version wiring, dependency locks, tests, and CI configuration.
3. Select `Alice RPG > Build Windows`.
4. Collect the generated `Builds/Windows` directory as one unit; the executable needs its adjacent data files. The build also creates `Documentation` with the release documents.
5. Run `./scripts/Package-WindowsRelease.ps1` from PowerShell to create a ZIP and its `.sha256` checksum in `Releases`.

The build command sets the product name, default 1280×720 full-screen mode, version `1.0.0`, player log output, and Mono scripting backend. Update the company name and version in `Assets/Editor/AliceRpgBuild.cs` before publication if they differ for your release.

## Test

Open `Window > General > Test Runner`, select **EditMode**, then run `AliceRpgGameTests`. The GitHub Actions workflow runs the release preflight first, then the same suite, and uploads its XML results even if the suite fails; it creates the Windows artifact only after a successful test run. Configure `UNITY_LICENSE` in repository secrets first.

## Release checklist

- Complete a playthrough on every difficulty and with a controller.
- Test loading each save slot and the backup recovery path.
- Verify 16:9 windowed/full-screen resolutions and high-contrast text.
- Confirm that the generated `Documentation` folder contains `README.md`, `CREDITS.md`, `PRIVACY.md`, `KNOWN_ISSUES.md`, and `BUILDING.md`.
- Publish the ZIP and its `.sha256` checksum together. Code-sign the executable when a publisher certificate is available.
- Replace the placeholder publisher name before distribution.
