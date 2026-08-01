using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Runtime;

namespace BannerlordCpuOptimizer.Profiling
{
    internal sealed class ProfilerTargetSpec
    {
        internal ProfilerTargetSpec(string assemblyName, string typeName, string methodName, string returnTypeName, IEnumerable<string> parameterTypeNames, string category, int sampleEvery, string expectedIlSha256 = null)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            MethodName = methodName;
            ReturnTypeName = returnTypeName;
            ParameterTypeNames = parameterTypeNames.ToArray();
            Category = category;
            SampleEvery = Math.Max(1, sampleEvery);
            ExpectedIlSha256 = expectedIlSha256;
        }

        internal string AssemblyName { get; }
        internal string TypeName { get; }
        internal string MethodName { get; }
        internal string ReturnTypeName { get; }
        internal IReadOnlyList<string> ParameterTypeNames { get; }
        internal string Category { get; }
        internal int SampleEvery { get; }
        internal string ExpectedIlSha256 { get; }

        internal MethodBase Resolve()
        {
            Assembly assembly = AssemblyProbe.FindLoaded(AssemblyName);
            Type type = assembly?.GetType(TypeName, false, false);
            if (type == null) { return null; }
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (!string.Equals(method.Name, MethodName, StringComparison.Ordinal)) { continue; }
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != ParameterTypeNames.Count) { continue; }
                bool match = true;
                for (int index = 0; index < parameters.Length; index++)
                {
                    if (!string.Equals(PatchGate.NormalizeTypeName(parameters[index].ParameterType), ParameterTypeNames[index], StringComparison.Ordinal)) { match = false; break; }
                }
                if (match && string.Equals(PatchGate.NormalizeTypeName(method.ReturnType), ReturnTypeName, StringComparison.Ordinal)) { return method; }
            }
            return null;
        }

        internal static ProfilerTargetSpec FromMethod(MethodInfo method, string category, int sampleEvery)
        {
            return new ProfilerTargetSpec(method.Module.Assembly.GetName().Name, method.DeclaringType.FullName, method.Name, PatchGate.NormalizeTypeName(method.ReturnType), method.GetParameters().Select(parameter => PatchGate.NormalizeTypeName(parameter.ParameterType)), category, sampleEvery);
        }

        internal static string FormatSignature(MethodBase method)
        {
            string parameters = string.Join(",", method.GetParameters().Select(parameter => PatchGate.NormalizeTypeName(parameter.ParameterType)));
            string returnType = method is MethodInfo info ? PatchGate.NormalizeTypeName(info.ReturnType) : "System.Void";
            return (method.DeclaringType?.FullName ?? "<global>") + "." + method.Name + "(" + parameters + "):" + returnType;
        }
    }
}
