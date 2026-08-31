# Known issues and release checks

- The project is validated against Unity 2022.3.62f1. Run EditMode tests and create the Windows build with that editor version, either locally with an activated editor or through the configured CI workflow.
- This is a Japanese-first release. The incomplete English toggle was removed so players are never presented with a partially translated experience.
- Controller layouts can differ by device and driver. Confirm the supported XInput-compatible hardware used for distribution.

Before public distribution, complete the release checklist in [BUILDING.md](BUILDING.md), verify controller layouts on target hardware, play through each difficulty, and replace the publisher name in `Assets/Editor/AliceRpgBuild.cs` if needed.
