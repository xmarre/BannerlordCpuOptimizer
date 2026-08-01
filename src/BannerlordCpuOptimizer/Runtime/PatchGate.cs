using System;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class PatchGate
    {
        internal static bool ValidateProfilerTarget(
            MethodBase method,
            ProfilerTargetSpec specification,
            bool allowUnknownProfilerTargets,
            out string reason)
        {
            return ValidateTarget(method, specification, allowUnknownProfilerTargets, out reason);
        }

        internal static bool ValidateTarget(
            MethodBase method,
            ProfilerTargetSpec specification,
            bool allowUnknownModule,
            out string reason)
        {
            if (method == null)
            {
                reason = "target method was not resolved";
                return false;
            }

            if (method.IsAbstract || method.ContainsGenericParameters)
            {
                reason = "abstract or open generic methods are not patchable";
                return false;
            }

            if (method.GetMethodBody() == null)
            {
                reason = "target has no managed method body";
                return false;
            }

            if (!SignatureMatches(method, specification, out reason))
            {
                return false;
            }

            string assemblyName = method.Module.Assembly.GetName().Name;
            Guid actualMvid = method.Module.ModuleVersionId;
            bool knownModule = KnownBuildCatalog.IsKnownModule(assemblyName, actualMvid);
            if (!knownModule && !allowUnknownModule)
            {
                reason = "unknown module MVID " + actualMvid.ToString("D");
                return false;
            }

            if (!string.IsNullOrEmpty(specification.ExpectedIlSha256))
            {
                string actualHash = MethodFingerprint.ComputeSha256(method);
                if (!string.Equals(actualHash, specification.ExpectedIlSha256, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "IL fingerprint mismatch: expected " + specification.ExpectedIlSha256
                        + ", actual " + (actualHash ?? "<none>");
                    return false;
                }
            }

            reason = knownModule
                ? "validated known build"
                : "validated exact signature on explicitly allowed unknown module";
            return true;
        }

        private static bool SignatureMatches(MethodBase method, ProfilerTargetSpec specification, out string reason)
        {
            if (!string.Equals(method.DeclaringType?.FullName, specification.TypeName, StringComparison.Ordinal))
            {
                reason = "declaring type mismatch";
                return false;
            }

            if (!string.Equals(method.Name, specification.MethodName, StringComparison.Ordinal))
            {
                reason = "method name mismatch";
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != specification.ParameterTypeNames.Count)
            {
                reason = "parameter count mismatch";
                return false;
            }

            for (int index = 0; index < parameters.Length; index++)
            {
                string actual = NormalizeTypeName(parameters[index].ParameterType);
                string expected = specification.ParameterTypeNames[index];
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    reason = "parameter " + index + " mismatch: expected " + expected + ", actual " + actual;
                    return false;
                }
            }

            MethodInfo methodInfo = method as MethodInfo;
            string actualReturn = methodInfo == null ? "System.Void" : NormalizeTypeName(methodInfo.ReturnType);
            if (!string.Equals(actualReturn, specification.ReturnTypeName, StringComparison.Ordinal))
            {
                reason = "return type mismatch: expected " + specification.ReturnTypeName + ", actual " + actualReturn;
                return false;
            }

            reason = null;
            return true;
        }

        internal static string NormalizeTypeName(Type type)
        {
            if (type.IsByRef)
            {
                return NormalizeTypeName(type.GetElementType()) + "&";
            }

            if (type.IsArray)
            {
                return NormalizeTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType)
            {
                string definition = type.GetGenericTypeDefinition().FullName ?? type.GetGenericTypeDefinition().Name;
                string arguments = string.Join(",", type.GetGenericArguments().Select(NormalizeTypeName));
                return definition + "[" + arguments + "]";
            }

            return type.FullName ?? type.Name;
        }
    }
}
