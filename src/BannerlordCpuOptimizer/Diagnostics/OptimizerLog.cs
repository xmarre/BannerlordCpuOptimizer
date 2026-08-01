using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BannerlordCpuOptimizer.Diagnostics
{
    internal static class OptimizerLog
    {
        private static readonly object Sync = new object();
        private static readonly HashSet<string> OnceKeys = new HashSet<string>(StringComparer.Ordinal);
        private static string _path;
        private static bool _verbose;

        internal static void Initialize(string directory, bool verbose)
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "BannerlordCpuOptimizer-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".log");
            _verbose = verbose;
            Info("Logger initialized.");
        }

        internal static void EarlyWrite(string message)
        {
            try
            {
                string directory = Runtime.PathProvider.LogDirectory;
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "BannerlordCpuOptimizer-early.log"), Format("WARN", message), Encoding.UTF8);
            }
            catch
            {
                // Logging must never interfere with game startup.
            }
        }

        internal static void Info(string message) => Write("INFO", message);
        internal static void Warn(string message) => Write("WARN", message);
        internal static void Error(string message) => Write("ERROR", message);

        internal static void Verbose(string message)
        {
            if (_verbose)
            {
                Write("TRACE", message);
            }
        }

        internal static void Once(string key, string level, string message)
        {
            lock (Sync)
            {
                if (!OnceKeys.Add(key))
                {
                    return;
                }
            }

            Write(level, message);
        }

        internal static void WriteExceptionOnce(string key, string context, Exception exception)
        {
            Once(key, "ERROR", context + ": " + exception.GetType().FullName + ": " + exception.Message);
        }

        internal static void Shutdown()
        {
            Info("Logger shutdown.");
            lock (Sync)
            {
                OnceKeys.Clear();
                _path = null;
            }
        }

        private static void Write(string level, string message)
        {
            string line = Format(level, message);
            try
            {
                lock (Sync)
                {
                    if (!string.IsNullOrEmpty(_path))
                    {
                        File.AppendAllText(_path, line, Encoding.UTF8);
                    }
                }
            }
            catch
            {
                // No profiler or optimizer failure may be caused by logging.
            }
        }

        private static string Format(string level, string message)
        {
            return "[" + DateTime.UtcNow.ToString("O") + "] [" + level + "] " + message + Environment.NewLine;
        }
    }
}
