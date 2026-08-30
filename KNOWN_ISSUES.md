# Known issues and release checks

- This workspace does not have an activated Unity license or the Unity editor image. Static source, package-lock, PowerShell-release-script, and contrast checks were completed, but Unity batch-mode play/build validation was not run here.
- The project targets Unity 2022.3 LTS. Use the CI workflow or an activated local editor to run EditMode tests and produce the Windows build.
- This is a Japanese-first release. The incomplete English toggle was removed so players are never presented with a partially translated experience.

Before public distribution, verify all controller layouts on target hardware, play through each difficulty, and replace the publisher name in `Assets/Editor/AliceRpgBuild.cs` if needed.
