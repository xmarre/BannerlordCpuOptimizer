using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class EventHandlerIlDiscovery
    {
        private static readonly OpCode[] OneByteOpcodes = new OpCode[0x100];
        private static readonly OpCode[] TwoByteOpcodes = new OpCode[0x100];

        static EventHandlerIlDiscovery()
        {
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is OpCode opcode)
                {
                    ushort value = unchecked((ushort)opcode.Value);
                    if (value < 0x100) { OneByteOpcodes[value] = opcode; }
                    else if ((value & 0xff00) == 0xfe00) { TwoByteOpcodes[value & 0xff] = opcode; }
                }
            }
        }

        internal static IEnumerable<MethodInfo> FindHandlers(Type campaignBehaviorType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            MethodInfo registerEvents = campaignBehaviorType.GetMethod("RegisterEvents", flags);
            byte[] il = registerEvents?.GetMethodBody()?.GetILAsByteArray();
            if (il == null) { yield break; }
            int position = 0;
            while (position < il.Length)
            {
                OpCode opcode = ReadOpcode(il, ref position);
                if (opcode.Size == 0) { yield break; }
                if (opcode.OperandType == OperandType.InlineMethod)
                {
                    if (position + 4 > il.Length) { yield break; }
                    int token = BitConverter.ToInt32(il, position);
                    position += 4;
                    if (opcode == OpCodes.Ldftn || opcode == OpCodes.Ldvirtftn)
                    {
                        MethodBase resolved = Resolve(registerEvents.Module, token, campaignBehaviorType);
                        if (resolved is MethodInfo method && method.GetMethodBody() != null) { yield return method; }
                    }
                    continue;
                }
                position += OperandSize(opcode.OperandType, il, position);
            }
        }

        private static MethodBase Resolve(Module module, int token, Type contextType)
        {
            try { return module.ResolveMethod(token, contextType.IsGenericType ? contextType.GetGenericArguments() : Type.EmptyTypes, Type.EmptyTypes); }
            catch { return null; }
        }

        private static OpCode ReadOpcode(byte[] il, ref int position)
        {
            byte value = il[position++];
            if (value != 0xfe) { return OneByteOpcodes[value]; }
            if (position >= il.Length) { return default(OpCode); }
            return TwoByteOpcodes[il[position++]];
        }

        private static int OperandSize(OperandType operandType, byte[] il, int position)
        {
            switch (operandType)
            {
                case OperandType.InlineNone: return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: return 1;
                case OperandType.InlineVar: return 2;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR: return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR: return 8;
                case OperandType.InlineSwitch:
                    if (position + 4 > il.Length) { return il.Length - position; }
                    return 4 + Math.Max(0, BitConverter.ToInt32(il, position)) * 4;
                default: return 0;
            }
        }
    }
}
