[CmdletBinding()]
param(
    [string]$BuildDirectory = "Builds/Windows",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$resolvedBuild = Resolve-Path -LiteralPath $BuildDirectory
$executable = Join-Path $resolvedBuild "AliceAndTheBrokenCrown.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Windows build executable was not found: $executable"
}

$releaseDirectory = Join-Path (Get-Location) "Releases"
New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
$archiveName = "Alice-and-the-Broken-Crown-$Version-win64.zip"
$archivePath = Join-Path $releaseDirectory $archiveName
$checksumPath = $archivePath + ".sha256"

if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
if (Test-Path -LiteralPath $checksumPath) { Remove-Item -LiteralPath $checksumPath -Force }

Compress-Archive -Path (Join-Path $resolvedBuild "*") -DestinationPath $archivePath -CompressionLevel Optimal
$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $archiveName" | Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Output "Packaged release: $archivePath"
Write-Output "SHA-256: $checksumPath"
