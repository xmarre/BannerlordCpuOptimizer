using System;
using System.Collections.Generic;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class KnownBuildCatalog
    {
        internal static readonly IReadOnlyDictionary<string, Guid> ModuleMvids =
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                ["TOR_Core"] = new Guid("933e2f8b-ec65-4b67-8a1d-153bcda29363"),
                ["TaleWorlds.CampaignSystem"] = new Guid("4b87d2d0-89dd-4989-adb4-69b0cca71136"),
                ["TaleWorlds.Engine"] = new Guid("3fb4feb9-c797-40f2-945f-74bbcd9e1994"),
                ["TaleWorlds.InputSystem"] = new Guid("73047213-64e8-486f-bb38-7a6bbf4bb4e3"),
                ["TaleWorlds.Library"] = new Guid("f951690e-4797-446e-a601-97418ef60bf5"),
                ["TaleWorlds.Localization"] = new Guid("d438bef0-6afc-4c84-86a7-1b2871b20c62"),
                ["TaleWorlds.MountAndBlade"] = new Guid("97564b07-7ad8-4ccd-9234-6076ec5623fe"),
                ["TaleWorlds.ObjectSystem"] = new Guid("a5961eb4-58ed-4baa-8baf-9af3384e1a58")
            };

        internal static bool IsKnownModule(string assemblyName, Guid mvid)
        {
            return ModuleMvids.TryGetValue(assemblyName, out Guid expected) && expected == mvid;
        }
    }
}
