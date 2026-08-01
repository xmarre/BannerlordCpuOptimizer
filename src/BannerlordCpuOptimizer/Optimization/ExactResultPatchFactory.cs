using System;
using System.Reflection;
using System.Reflection.Emit;

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class ExactResultPatchMethods
    {
        internal ExactResultPatchMethods(Type patchType, MethodInfo prefix, MethodInfo postfix)
        {
            PatchType = patchType;
            Prefix = prefix;
            Postfix = postfix;
        }

        internal Type PatchType { get; }
        internal MethodInfo Prefix { get; }
        internal MethodInfo Postfix { get; }
    }

    internal static class ExactResultPatchFactory
    {
        private static readonly object Sync = new object();
        private static readonly ModuleBuilder Module;
        private static int _nextTypeId;

        static ExactResultPatchFactory()
        {
            var assemblyName = new AssemblyName("BannerlordCpuOptimizer.RuntimePatches");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run);
            Module = assembly.DefineDynamicModule(assemblyName.Name);
        }

        internal static ExactResultPatchMethods Create(
            Type resultType,
            Type stateType,
            MethodInfo beginBridge,
            MethodInfo completeBridge)
        {
            Validate(resultType, stateType, beginBridge, completeBridge);

            lock (Sync)
            {
                string typeName = "BannerlordCpuOptimizer.RuntimePatches.ExactResultPatch_"
                    + (++_nextTypeId).ToString();
                TypeBuilder typeBuilder = Module.DefineType(
                    typeName,
                    TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);

                MethodBuilder prefix = BuildPrefix(
                    typeBuilder,
                    resultType,
                    stateType,
                    beginBridge);
                MethodBuilder postfix = BuildPostfix(
                    typeBuilder,
                    resultType,
                    stateType,
                    completeBridge);

                Type patchType = typeBuilder.CreateType();
                MethodInfo prefixMethod = patchType.GetMethod(prefix.Name, BindingFlags.Public | BindingFlags.Static);
                MethodInfo postfixMethod = patchType.GetMethod(postfix.Name, BindingFlags.Public | BindingFlags.Static);
                if (prefixMethod == null || postfixMethod == null)
                {
                    throw new MissingMethodException("Could not resolve the emitted exact-result patch methods.");
                }

                return new ExactResultPatchMethods(patchType, prefixMethod, postfixMethod);
            }
        }

        private static MethodBuilder BuildPrefix(
            TypeBuilder typeBuilder,
            Type resultType,
            Type stateType,
            MethodInfo beginBridge)
        {
            MethodBuilder method = typeBuilder.DefineMethod(
                "Prefix",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(bool),
                new[]
                {
                    typeof(string),
                    resultType.MakeByRefType(),
                    stateType.MakeByRefType()
                });
            method.DefineParameter(1, ParameterAttributes.None, "__0");
            method.DefineParameter(2, ParameterAttributes.None, "__result");
            method.DefineParameter(3, ParameterAttributes.Out, "__state");

            ILGenerator il = method.GetILGenerator();
            LocalBuilder cachedResult = il.DeclareLocal(typeof(object));
            LocalBuilder runOriginal = il.DeclareLocal(typeof(bool));
            Label originalPath = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca_S, cachedResult);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, beginBridge);
            il.Emit(OpCodes.Stloc, runOriginal);

            il.Emit(OpCodes.Ldloc, runOriginal);
            il.Emit(OpCodes.Brtrue_S, originalPath);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, cachedResult);
            il.Emit(OpCodes.Castclass, resultType);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(originalPath);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);
            return method;
        }

        private static MethodBuilder BuildPostfix(
            TypeBuilder typeBuilder,
            Type resultType,
            Type stateType,
            MethodInfo completeBridge)
        {
            MethodBuilder method = typeBuilder.DefineMethod(
                "Postfix",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(void),
                new[]
                {
                    typeof(string),
                    resultType,
                    stateType
                });
            method.DefineParameter(1, ParameterAttributes.None, "__0");
            method.DefineParameter(2, ParameterAttributes.None, "__result");
            method.DefineParameter(3, ParameterAttributes.None, "__state");

            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, completeBridge);
            il.Emit(OpCodes.Ret);
            return method;
        }

        private static void Validate(
            Type resultType,
            Type stateType,
            MethodInfo beginBridge,
            MethodInfo completeBridge)
        {
            if (resultType == null || resultType == typeof(void) || resultType.IsValueType)
            {
                throw new ArgumentException("The exact-result patch requires a reference-type return value.", nameof(resultType));
            }

            if (stateType == null || stateType == typeof(void) || stateType.IsByRef || stateType.ContainsGenericParameters)
            {
                throw new ArgumentException("The exact-result patch requires a closed state type.", nameof(stateType));
            }

            ValidateBridge(
                beginBridge,
                typeof(bool),
                new[]
                {
                    typeof(string),
                    typeof(object).MakeByRefType(),
                    stateType.MakeByRefType()
                },
                "begin");
            ValidateBridge(
                completeBridge,
                typeof(void),
                new[]
                {
                    typeof(string),
                    typeof(object),
                    stateType
                },
                "complete");
        }

        private static void ValidateBridge(
            MethodInfo method,
            Type returnType,
            Type[] parameterTypes,
            string role)
        {
            if (method == null || !method.IsStatic || !method.IsPublic || method.ContainsGenericParameters)
            {
                throw new ArgumentException("The " + role + " bridge must be a public, static, closed method.");
            }

            if (method.ReturnType != returnType)
            {
                throw new ArgumentException("The " + role + " bridge has the wrong return type.");
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
            {
                throw new ArgumentException("The " + role + " bridge has the wrong parameter count.");
            }

            for (int index = 0; index < parameterTypes.Length; index++)
            {
                if (parameters[index].ParameterType != parameterTypes[index])
                {
                    throw new ArgumentException("The " + role + " bridge parameter " + index + " has the wrong type.");
                }
            }
        }
    }
}
