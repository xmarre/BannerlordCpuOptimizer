using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Configuration
{
    internal static class SettingsLoader
    {
        internal static OptimizerSettings LoadOrCreate(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            if (!File.Exists(path))
            {
                OptimizerSettings defaults = OptimizerSettings.CreateDefault();
                Save(path, defaults);
                return defaults;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(OptimizerSettings));
                    var settings = serializer.ReadObject(stream) as OptimizerSettings ?? OptimizerSettings.CreateDefault();
                    settings.Normalize();
                    return settings;
                }
            }
            catch (Exception exception)
            {
                string invalidPath = path + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                try
                {
                    File.Copy(path, invalidPath, true);
                }
                catch
                {
                    // Preserve the original exception and continue with safe defaults.
                }

                OptimizerLog.EarlyWrite("Settings could not be read; safe defaults are active. " + exception.GetType().FullName + ": " + exception.Message);
                return OptimizerSettings.CreateDefault();
            }
        }

        internal static void Save(string path, OptimizerSettings settings)
        {
            string temporaryPath = path + ".tmp";
            using (FileStream stream = File.Create(temporaryPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(OptimizerSettings));
                serializer.WriteObject(stream, settings);
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
    }
}
