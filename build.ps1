[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $root
try {
    python '.\tests\check_profiler_only_invariants.py'
    python '.\tests\check_source_structure.py'
    dotnet restore '.\BannerlordCpuOptimizer.sln'
    dotnet build '.\BannerlordCpuOptimizer.sln' `
        --no-restore `
        --configuration $Configuration `
        -p:ContinuousIntegrationBuild=true
    dotnet run `
        --project '.\tests\BannerlordCpuOptimizer.HarmonyTeardownHarness\BannerlordCpuOptimizer.HarmonyTeardownHarness.csproj' `
        --no-build `
        --no-restore `
        --configuration $Configuration
} finally {
    Pop-Location
}

$output = Join-Path $root 'module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll'
if (-not (Test-Path $output)) {
    throw "Build completed without the expected output: $output"
}
Write-Host "Built: $output"
