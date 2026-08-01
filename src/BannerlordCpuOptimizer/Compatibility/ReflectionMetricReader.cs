using System;
using System.Collections;
using System.Reflection;

namespace BannerlordCpuOptimizer.Compatibility
{
    internal static class ReflectionMetricReader
    {
        internal static int ReadCount(object instance, params string[] memberNames)
        {
            if (instance == null)
            {
                return -1;
            }

            foreach (string memberName in memberNames)
            {
                object value = ReadMember(instance, memberName);
                if (value == null)
                {
                    continue;
                }

                if (value is ICollection collection)
                {
                    return collection.Count;
                }

                PropertyInfo count = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (count != null && count.PropertyType == typeof(int))
                {
                    return (int)count.GetValue(value, null);
                }
            }

            return -1;
        }

        internal static string ReadString(object instance, params string[] memberNames)
        {
            if (instance == null)
            {
                return null;
            }

            foreach (string memberName in memberNames)
            {
                object value = ReadMember(instance, memberName);
                if (value != null)
                {
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

        internal static double? TryReadMapZoom(bool allowUnvalidatedOptionalMetrics)
        {
            if (!allowUnvalidatedOptionalMetrics)
            {
                return null;
            }

            try
            {
                Type mapScreenType = FindType("SandBox.View.Map.MapScreen");
                if (mapScreenType == null)
                {
                    return null;
                }

                object mapScreen = ReadStaticMember(mapScreenType, "Instance");
                if (mapScreen == null)
                {
                    return null;
                }

                foreach (string name in new[] { "Zoom", "CurrentZoom", "MapZoom", "CameraZoom" })
                {
                    object value = ReadMember(mapScreen, name);
                    if (TryConvertDouble(value, out double zoom))
                    {
                        return zoom;
                    }
                }

                object camera = ReadMember(mapScreen, "MapCamera") ?? ReadMember(mapScreen, "Camera");
                if (camera != null)
                {
                    foreach (string name in new[] { "Zoom", "FieldOfView", "HorizontalFov", "VerticalFov" })
                    {
                        object value = ReadMember(camera, name);
                        if (TryConvertDouble(value, out double zoom))
                        {
                            return zoom;
                        }
                    }
                }
            }
            catch
            {
                // Optional metric; absence must not affect profiling.
            }

            return null;
        }

        private static object ReadMember(object instance, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = instance.GetType().GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance, null);
            }

            FieldInfo field = instance.GetType().GetField(name, flags);
            return field?.GetValue(instance);
        }

        private static object ReadStaticMember(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(null, null);
            }

            return type.GetField(name, flags)?.GetValue(null);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool TryConvertDouble(object value, out double result)
        {
            switch (value)
            {
                case byte byteValue:
                    result = byteValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = doubleValue;
                    return true;
                case decimal decimalValue:
                    result = (double)decimalValue;
                    return true;
                default:
                    result = 0.0;
                    return false;
            }
        }
    }
}
