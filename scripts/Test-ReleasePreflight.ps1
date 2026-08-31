[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot

function Require-Path([string]$path)
{
    if (-not (Test-Path -LiteralPath $path)) { throw "Required release input is missing: $path" }
}

$requiredPaths = @(
    "Assets/Branding/AliceAppIcon.png",
    "Assets/Editor/AliceRpgBuild.cs",
    "Assets/Scripts/AliceRpgGame.cs",
    "Assets/Scripts/AliceRpgGame.Models.cs",
    "Assets/Scripts/AliceRpgGame.Audio.cs",
    "Assets/Scripts/AliceRpgGame.Settings.cs",
    "Assets/Scripts/AliceRpgGame.Persistence.cs",
    "Assets/Scripts/AliceRpgGame.UI.cs",
    "Assets/Scripts/AliceRpgGame.Textures.cs",
    "Assets/Tests/Editor/AliceRpgGameTests.cs",
    "ProjectSettings/ProjectVersion.txt",
    "ProjectSettings/InputManager.asset",
    "Packages/manifest.json",
    "Packages/packages-lock.json",
    ".github/workflows/unity.yml",
    "scripts/Package-WindowsRelease.ps1",
    "README.md",
    "CREDITS.md",
    "PRIVACY.md",
    "KNOWN_ISSUES.md",
    "BUILDING.md"
)
foreach ($path in $requiredPaths) { Require-Path $path }

$projectVersion = (Get-Content -LiteralPath "ProjectSettings/ProjectVersion.txt" | Select-String "^m_EditorVersion:").ToString().Split(":")[1].Trim()
if ([string]::IsNullOrWhiteSpace($projectVersion)) { throw "Unity editor version is missing from ProjectVersion.txt." }

$gameSource = Get-Content -LiteralPath "Assets/Scripts/AliceRpgGame.cs" -Raw
$buildSource = Get-Content -LiteralPath "Assets/Editor/AliceRpgBuild.cs" -Raw
$versionMatch = [regex]::Match($gameSource, 'public const string Version\s*=\s*"(?<version>[^"]+)"')
if (-not $versionMatch.Success) { throw "AliceRpgBuildInfo.Version was not found." }
$gameVersion = $versionMatch.Groups["version"].Value
if ($buildSource -notmatch "ProductVersion\s*=\s*AliceRpgBuildInfo\.Version") { throw "Windows build version is not linked to AliceRpgBuildInfo.Version." }

$manifest = Get-Content -LiteralPath "Packages/manifest.json" -Raw | ConvertFrom-Json
$lock = Get-Content -LiteralPath "Packages/packages-lock.json" -Raw | ConvertFrom-Json
$missingLockEntries = @($manifest.dependencies.psobject.Properties | Where-Object { $null -eq $lock.dependencies.$($_.Name) })
if ($missingLockEntries.Count -gt 0)
{
    throw "packages-lock.json is missing: $($missingLockEntries.Name -join ', ')"
}

$workflow = Get-Content -LiteralPath ".github/workflows/unity.yml" -Raw
foreach ($requiredWorkflowValue in @("game-ci/unity-test-runner@v4", "game-ci/unity-builder@v4", "AliceRpgBuild.BuildWindows", "actions/upload-artifact@v4"))
{
    if ($workflow -notmatch [regex]::Escape($requiredWorkflowValue)) { throw "CI workflow is missing: $requiredWorkflowValue" }
}

$packageScriptErrors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path "scripts/Package-WindowsRelease.ps1"), [ref]$null, [ref]$packageScriptErrors)
if ($null -ne $packageScriptErrors -and $packageScriptErrors.Count -gt 0)
{
    throw "Package-WindowsRelease.ps1 has syntax errors: $($packageScriptErrors[0].Message)"
}

$testCount = @(Select-String -LiteralPath "Assets/Tests/Editor/AliceRpgGameTests.cs" -Pattern "^\s*\[Test\]").Count
if ($testCount -lt 1) { throw "No EditMode tests were found." }

Write-Output "Release preflight passed"
Write-Output "Unity editor: $projectVersion"
Write-Output "Game version: $gameVersion"
Write-Output "EditMode tests: $testCount"
