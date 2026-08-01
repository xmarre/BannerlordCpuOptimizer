using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Runtime
{
    internal sealed class AssemblyIdentity
    {
        internal string Name { get; set; }
        internal string AssemblyVersion { get; set; }
        internal string FileVersion { get; set; }
        internal Guid Mvid { get; set; }
        internal string Location { get; set; }

        public override string ToString()
        {
            return Name + " assembly=" + AssemblyVersion + " file=" + FileVersion + " mvid=" + Mvid.ToString("D");
        }
    }

    internal static class AssemblyProbe
    {
        internal static IReadOnlyList<AssemblyIdentity> CaptureLoadedAssemblies()
        {
            var result = new List<AssemblyIdentity>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Array.Sort(assemblies, (left, right) => string.CompareOrdinal(left.GetName().Name, right.GetName().Name));

            foreach (Assembly assembly in assemblies)
            {
                string name = assembly.GetName().Name;
                if (!IsRelevant(name))
                {
                    continue;
                }

                try
                {
                    result.Add(Capture(assembly));
                }
                catch (Exception exception)
                {
                    OptimizerLog.WriteExceptionOnce("assembly-probe-" + name, "Could not inspect assembly " + name, exception);
                }
            }

            return result;
        }

        internal static AssemblyIdentity Capture(Assembly assembly)
        {
            string location = SafeLocation(assembly);
            string fileVersion = "unknown";
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(location);
                fileVersion = versionInfo.FileVersion ?? "unknown";
            }

            return new AssemblyIdentity
            {
                Name = assembly.GetName().Name,
                AssemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown",
                FileVersion = fileVersion,
                Mvid = assembly.ManifestModule.ModuleVersionId,
                Location = location
            };
        }

        internal static Assembly FindLoaded(string simpleName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, simpleName, StringComparison.Ordinal));
        }

        private static bool IsRelevant(string name)
        {
            return name.StartsWith("TaleWorlds.", StringComparison.Ordinal)
                || name.StartsWith("TOR_", StringComparison.Ordinal)
                || string.Equals(name, "BannerlordCpuOptimizer", StringComparison.Ordinal)
                || string.Equals(name, "0Harmony", StringComparison.Ordinal);
        }

        private static string SafeLocation(Assembly assembly)
        {
            try
            {
                return assembly.IsDynamic ? string.Empty : assembly.Location;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
