using System;
using System.IO;
using System.Reflection;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class PathProvider
    {
        internal static string UserRoot
        {
            get
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(documents, "Mount and Blade II Bannerlord", "Configs", "BannerlordCpuOptimizer");
            }
        }

        internal static string UserSettingsPath => Path.Combine(UserRoot, "settings.json");

        internal static string ModuleSettingsPath
        {
            get
            {
                try
                {
                    string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    if (string.IsNullOrEmpty(assemblyLocation))
                    {
                        return null;
                    }

                    DirectoryInfo assemblyDirectory = Directory.GetParent(assemblyLocation);
                    DirectoryInfo moduleRoot = assemblyDirectory?.Parent?.Parent;
                    return moduleRoot == null
                        ? null
                        : Path.Combine(moduleRoot.FullName, "ModuleData", "BannerlordCpuOptimizer", "settings.json");
                }
                catch
                {
                    return null;
                }
            }
        }

        internal static string ResolveSettingsPath()
        {
            string moduleSettingsPath = ModuleSettingsPath;
            if (!string.IsNullOrEmpty(moduleSettingsPath) && File.Exists(moduleSettingsPath))
            {
                return moduleSettingsPath;
            }

            return UserSettingsPath;
        }

        internal static string LogDirectory => Path.Combine(UserRoot, "logs");
        internal static string ReportDirectory => Path.Combine(UserRoot, "reports");
    }
}
