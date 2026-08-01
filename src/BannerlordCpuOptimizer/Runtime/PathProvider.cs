using System;
using System.IO;

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

        internal static string SettingsPath => Path.Combine(UserRoot, "settings.json");
        internal static string LogDirectory => Path.Combine(UserRoot, "logs");
        internal static string ReportDirectory => Path.Combine(UserRoot, "reports");
    }
}
