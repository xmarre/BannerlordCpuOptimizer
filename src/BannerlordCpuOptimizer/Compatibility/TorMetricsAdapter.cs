using System;
using System.Collections;
using System.Reflection;
using BannerlordCpuOptimizer.Runtime;
using TaleWorlds.MountAndBlade;
using GameMission = TaleWorlds.MountAndBlade.Mission;

namespace BannerlordCpuOptimizer.Compatibility
{
    internal static class TorMetricsAdapter
    {
        private const string AbilityManagerTypeName = "TOR_Core.AbilitySystem.AbilityManagerMissionLogic";
        private static Type _abilityManagerType;
        private static FieldInfo _activeSpellSessions;
        private static bool _resolved;
        private static bool _allowUnknown;

        internal static void Configure(bool allowUnknown)
        {
            _allowUnknown = allowUnknown;
        }

        internal static int ReadActiveSpellOrEffectCount(GameMission mission)
        {
            if (mission == null)
            {
                return -1;
            }

            Resolve();
            if (_abilityManagerType == null || _activeSpellSessions == null)
            {
                return -1;
            }

            try
            {
                foreach (MissionBehavior behavior in mission.MissionBehaviors)
                {
                    if (behavior != null && _abilityManagerType.IsInstanceOfType(behavior))
                    {
                        return ReadCollectionCount(_activeSpellSessions.GetValue(behavior));
                    }
                }
            }
            catch
            {
                return -1;
            }

            return 0;
        }

        internal static void ClearInstanceCache()
        {
            // Metadata only is cached. No MissionBehavior instance is retained.
        }

        internal static void ClearAll()
        {
            _abilityManagerType = null;
            _activeSpellSessions = null;
            _resolved = false;
            _allowUnknown = false;
        }

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, "TOR_Core", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_allowUnknown && !KnownBuildCatalog.IsKnownModule("TOR_Core", assembly.ManifestModule.ModuleVersionId))
                {
                    return;
                }

                Type candidate = assembly.GetType(AbilityManagerTypeName, false, false);
                if (candidate == null)
                {
                    return;
                }

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo sessions = candidate.GetField("_activeSpellSessions", flags);
                if (sessions == null)
                {
                    return;
                }

                _abilityManagerType = candidate;
                _activeSpellSessions = sessions;
                return;
            }
        }

        private static int ReadCollectionCount(object value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            PropertyInfo property = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property != null && property.PropertyType == typeof(int) ? (int)property.GetValue(value, null) : -1;
        }
    }
}
