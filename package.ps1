[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$module = Join-Path $root 'module\BannerlordCpuOptimizer'
$dll = Join-Path $module 'bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll'
if (-not (Test-Path $dll)) {
    throw "Build output is missing: $dll"
}

$artifactDir = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$zip = Join-Path $artifactDir 'BannerlordCpuOptimizer-v0.1.2-profiler-only.zip'
if (Test-Path $zip) {
    Remove-Item $zip -Force
}

$staging = Join-Path $artifactDir 'staging'
Remove-Item $staging -Force -Recurse -ErrorAction SilentlyContinue
$destination = Join-Path $staging 'Modules\BannerlordCpuOptimizer'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item (Join-Path $module '*') $destination -Recurse -Force
Get-ChildItem $destination -Recurse -Include '*.pdb','*.xml','*.deps.json' | Where-Object {
    $_.Name -ne 'SubModule.xml'
} | Remove-Item -Force
Compress-Archive -Path (Join-Path $staging 'Modules') -DestinationPath $zip -CompressionLevel Optimal
Remove-Item $staging -Force -Recurse

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$hashFile = Join-Path $artifactDir 'SHA256SUMS.txt'
"$hash  $(Split-Path $zip -Leaf)" | Set-Content -Path $hashFile -Encoding ascii
Write-Host "Packaged: $zip"
Write-Host "SHA-256: $hash"
