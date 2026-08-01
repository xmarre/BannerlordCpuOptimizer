# Reproducible build

## Inputs

- Repository source at the tag or commit to build.
- .NET 8 SDK.
- Python 3.
- NuGet access.

The project pins `Bannerlord.ReferenceAssemblies.Core` v1.3.15.110062 and `Lib.Harmony` v2.3.3. It never commits or packages TaleWorlds, TOR, or Harmony runtime binaries.

## Commands

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
Get-Content .\artifacts\SHA256SUMS.txt
```

Expected DLL:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
```

Expected package:

```text
artifacts\BannerlordCpuOptimizer-v0.1.0-profiler-only.zip
```

GitHub Actions executes the same source gates, restore, build, packaging, archive-content checks, and forbidden-runtime-DLL checks before publishing v0.1.0.
