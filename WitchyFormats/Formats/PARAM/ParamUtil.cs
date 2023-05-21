using System;
using System.Collections.Generic;
using SoulsFormats;

namespace WitchyFormats
{
    internal static class ParamUtil
    {
        public static string GetDefaultFormat(PARAMDEF.DefType type)
        {
            switch (type)
            {
                case PARAMDEF.DefType.s8: return "%d";
                case PARAMDEF.DefType.u8: return "%d";
                case PARAMDEF.DefType.s16: return "%d";
                case PARAMDEF.DefType.u16: return "%d";
                case PARAMDEF.DefType.s32: return "%d";
                case PARAMDEF.DefType.u32: return "%d";
                case PARAMDEF.DefType.b32: return "%d";
                case PARAMDEF.DefType.f32: return "%f";
                case PARAMDEF.DefType.angle32: return "%f";
                case PARAMDEF.DefType.f64: return "%f";
                case PARAMDEF.DefType.dummy8: return "";
                case PARAMDEF.DefType.fixstr: return "%d";
                case PARAMDEF.DefType.fixstrW: return "%d";

                default:
                    throw new NotImplementedException($"No default format specified for {nameof(PARAMDEF.DefType)}.{type}");
            }
        }

        private static readonly Dictionary<PARAMDEF.DefType, float> fixedDefaults = new Dictionary<PARAMDEF.DefType, float>
        {
            [PARAMDEF.DefType.s8] = 0,
            [PARAMDEF.DefType.u8] = 0,
            [PARAMDEF.DefType.s16] = 0,
            [PARAMDEF.DefType.u16] = 0,
            [PARAMDEF.DefType.s32] = 0,
            [PARAMDEF.DefType.u32] = 0,
            [PARAMDEF.DefType.b32] = 0,
            [PARAMDEF.DefType.f32] = 0,
            [PARAMDEF.DefType.angle32] = 0,
            [PARAMDEF.DefType.f64] = 0,
            [PARAMDEF.DefType.dummy8] = 0,
            [PARAMDEF.DefType.fixstr] = 0,
            [PARAMDEF.DefType.fixstrW] = 0,
        };

        private static readonly Dictionary<PARAMDEF.DefType, object> variableDefaults = new Dictionary<PARAMDEF.DefType, object>
        {
            [PARAMDEF.DefType.s8] = 0,
            [PARAMDEF.DefType.u8] = 0,
            [PARAMDEF.DefType.s16] = 0,
            [PARAMDEF.DefType.u16] = 0,
            [PARAMDEF.DefType.s32] = 0,
            [PARAMDEF.DefType.u32] = 0,
            [PARAMDEF.DefType.b32] = 0,
            [PARAMDEF.DefType.f32] = 0f,
            [PARAMDEF.DefType.angle32] = 0f,
            [PARAMDEF.DefType.f64] = 0d,
            [PARAMDEF.DefType.dummy8] = null,
            [PARAMDEF.DefType.fixstr] = null,
            [PARAMDEF.DefType.fixstrW] = null,
        };

        public static object GetDefaultDefault(PARAMDEF def, PARAMDEF.DefType type)
        {
            if (def?.VariableEditorValueTypes ?? false)
                return variableDefaults[type];
            return fixedDefaults[type];
        }

        private static readonly Dictionary<PARAMDEF.DefType, float> fixedMinimums = new Dictionary<PARAMDEF.DefType, float>
        {
            [PARAMDEF.DefType.s8] = sbyte.MinValue,
            [PARAMDEF.DefType.u8] = byte.MinValue,
            [PARAMDEF.DefType.s16] = short.MinValue,
            [PARAMDEF.DefType.u16] = ushort.MinValue,
            [PARAMDEF.DefType.s32] = -2147483520, // Smallest representable float greater than int.MinValue
            [PARAMDEF.DefType.u32] = uint.MinValue,
            [PARAMDEF.DefType.b32] = 0,
            [PARAMDEF.DefType.f32] = float.MinValue,
            [PARAMDEF.DefType.angle32] = float.MinValue,
            [PARAMDEF.DefType.f64] = float.MinValue,
            [PARAMDEF.DefType.dummy8] = 0,
            [PARAMDEF.DefType.fixstr] = -1,
            [PARAMDEF.DefType.fixstrW] = -1,
        };

        private static readonly Dictionary<PARAMDEF.DefType, object> variableMinimums = new Dictionary<PARAMDEF.DefType, object>
        {
            [PARAMDEF.DefType.s8] = (int)sbyte.MinValue,
            [PARAMDEF.DefType.u8] = (int)byte.MinValue,
            [PARAMDEF.DefType.s16] = (int)short.MinValue,
            [PARAMDEF.DefType.u16] = (int)ushort.MinValue,
            [PARAMDEF.DefType.s32] = int.MinValue,
            [PARAMDEF.DefType.u32] = (int)uint.MinValue,
            [PARAMDEF.DefType.b32] = 0,
            [PARAMDEF.DefType.f32] = float.MinValue,
            [PARAMDEF.DefType.angle32] = float.MinValue,
            [PARAMDEF.DefType.f64] = double.MinValue,
            [PARAMDEF.DefType.dummy8] = null,
            [PARAMDEF.DefType.fixstr] = null,
            [PARAMDEF.DefType.fixstrW] = null,
        };

        public static object GetDefaultMinimum(PARAMDEF def, PARAMDEF.DefType type)
        {
            if (def?.VariableEditorValueTypes ?? false)
                return variableMinimums[type];
            return fixedMinimums[type];
        }

        private static readonly Dictionary<PARAMDEF.DefType, float> fixedMaximums = new Dictionary<PARAMDEF.DefType, float>
        {
            [PARAMDEF.DefType.s8] = sbyte.MaxValue,
            [PARAMDEF.DefType.u8] = byte.MaxValue,
            [PARAMDEF.DefType.s16] = short.MaxValue,
            [PARAMDEF.DefType.u16] = ushort.MaxValue,
            [PARAMDEF.DefType.s32] = 2147483520, // Largest representable float less than int.MaxValue
            [PARAMDEF.DefType.u32] = 4294967040, // Largest representable float less than uint.MaxValue
            [PARAMDEF.DefType.b32] = 1,
            [PARAMDEF.DefType.f32] = float.MaxValue,
            [PARAMDEF.DefType.angle32] = float.MaxValue,
            [PARAMDEF.DefType.f64] = float.MaxValue,
            [PARAMDEF.DefType.dummy8] = 0,
            [PARAMDEF.DefType.fixstr] = 1000000000,
            [PARAMDEF.DefType.fixstrW] = 1000000000,
        };

        private static readonly Dictionary<PARAMDEF.DefType, object> variableMaximums = new Dictionary<PARAMDEF.DefType, object>
        {
            [PARAMDEF.DefType.s8] = (int)sbyte.MaxValue,
            [PARAMDEF.DefType.u8] = (int)byte.MaxValue,
            [PARAMDEF.DefType.s16] = (int)short.MaxValue,
            [PARAMDEF.DefType.u16] = (int)ushort.MaxValue,
            [PARAMDEF.DefType.s32] = int.MaxValue,
            [PARAMDEF.DefType.u32] = int.MaxValue, // Yes, u32 uses signed int too (usually)
            [PARAMDEF.DefType.b32] = 1,
            [PARAMDEF.DefType.f32] = float.MaxValue,
            [PARAMDEF.DefType.angle32] = float.MaxValue,
            [PARAMDEF.DefType.f64] = double.MaxValue,
            [PARAMDEF.DefType.dummy8] = null,
            [PARAMDEF.DefType.fixstr] = null,
            [PARAMDEF.DefType.fixstrW] = null,
        };

        public static object GetDefaultMaximum(PARAMDEF def, PARAMDEF.DefType type)
        {
            if (def?.VariableEditorValueTypes ?? false)
                return variableMaximums[type];
            return fixedMaximums[type];
        }

        private static readonly Dictionary<PARAMDEF.DefType, float> fixedIncrements = new Dictionary<PARAMDEF.DefType, float>
        {
            [PARAMDEF.DefType.s8] = 1,
            [PARAMDEF.DefType.u8] = 1,
            [PARAMDEF.DefType.s16] = 1,
            [PARAMDEF.DefType.u16] = 1,
            [PARAMDEF.DefType.s32] = 1,
            [PARAMDEF.DefType.u32] = 1,
            [PARAMDEF.DefType.b32] = 1,
            [PARAMDEF.DefType.f32] = 0.01f,
            [PARAMDEF.DefType.angle32] = 0.01f,
            [PARAMDEF.DefType.f64] = 0.01f,
            [PARAMDEF.DefType.dummy8] = 0,
            [PARAMDEF.DefType.fixstr] = 1,
            [PARAMDEF.DefType.fixstrW] = 1,
        };

        private static readonly Dictionary<PARAMDEF.DefType, object> variableIncrements = new Dictionary<PARAMDEF.DefType, object>
        {
            [PARAMDEF.DefType.s8] = 1,
            [PARAMDEF.DefType.u8] = 1,
            [PARAMDEF.DefType.s16] = 1,
            [PARAMDEF.DefType.u16] = 1,
            [PARAMDEF.DefType.s32] = 1,
            [PARAMDEF.DefType.u32] = 1,
            [PARAMDEF.DefType.b32] = 1,
            [PARAMDEF.DefType.f32] = 0.01f,
            [PARAMDEF.DefType.angle32] = 0.01f,
            [PARAMDEF.DefType.f64] = 0.01d,
            [PARAMDEF.DefType.dummy8] = null,
            [PARAMDEF.DefType.fixstr] = null,
            [PARAMDEF.DefType.fixstrW] = null,
        };

        public static object GetDefaultIncrement(PARAMDEF def, PARAMDEF.DefType type)
        {
            if (def?.VariableEditorValueTypes ?? false)
                return variableIncrements[type];
            return fixedIncrements[type];
        }

        public static PARAMDEF.EditFlags GetDefaultEditFlags(PARAMDEF.DefType type)
        {
            switch (type)
            {
                case PARAMDEF.DefType.s8: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.u8: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.s16: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.u16: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.s32: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.u32: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.b32: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.f32: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.angle32: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.f64: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.dummy8: return PARAMDEF.EditFlags.None;
                case PARAMDEF.DefType.fixstr: return PARAMDEF.EditFlags.Wrap;
                case PARAMDEF.DefType.fixstrW: return PARAMDEF.EditFlags.Wrap;

                default:
                    throw new NotImplementedException($"No default edit flags specified for {nameof(PARAMDEF.DefType)}.{type}");
            }
        }

        public static bool IsArrayType(PARAMDEF.DefType type)
        {
            switch (type)
            {
                case PARAMDEF.DefType.dummy8:
                case PARAMDEF.DefType.fixstr:
                case PARAMDEF.DefType.fixstrW:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsBitType(PARAMDEF.DefType type)
        {
            switch (type)
            {
                case PARAMDEF.DefType.u8:
                case PARAMDEF.DefType.u16:
                case PARAMDEF.DefType.u32:
                case PARAMDEF.DefType.dummy8:
                    return true;

                default:
                    return false;
            }
        }

        public static int GetValueSize(PARAMDEF.DefType type)
        {
            switch (type)
            {
                case PARAMDEF.DefType.s8: return 1;
                case PARAMDEF.DefType.u8: return 1;
                case PARAMDEF.DefType.s16: return 2;
                case PARAMDEF.DefType.u16: return 2;
                case PARAMDEF.DefType.s32: return 4;
                case PARAMDEF.DefType.u32: return 4;
                case PARAMDEF.DefType.b32: return 4;
                case PARAMDEF.DefType.f32: return 4;
                case PARAMDEF.DefType.angle32: return 4;
                case PARAMDEF.DefType.f64: return 8;
                case PARAMDEF.DefType.dummy8: return 1;
                case PARAMDEF.DefType.fixstr: return 1;
                case PARAMDEF.DefType.fixstrW: return 2;

                default:
                    throw new NotImplementedException($"No value size specified for {nameof(PARAMDEF.DefType)}.{type}");
            }
        }

        public static object ConvertDefaultValue(PARAMDEF.Field field)
        {
            switch (field.DisplayType)
            {
                case PARAMDEF.DefType.s8: return Convert.ToSByte(field.Default);
                case PARAMDEF.DefType.u8: return Convert.ToByte(field.Default);
                case PARAMDEF.DefType.s16: return Convert.ToInt16(field.Default);
                case PARAMDEF.DefType.u16: return Convert.ToUInt16(field.Default);
                case PARAMDEF.DefType.s32: return Convert.ToInt32(field.Default);
                case PARAMDEF.DefType.u32: return Convert.ToUInt32(field.Default);
                case PARAMDEF.DefType.b32: return Convert.ToInt32(field.Default);
                case PARAMDEF.DefType.f32: return Convert.ToSingle(field.Default);
                case PARAMDEF.DefType.angle32: return Convert.ToSingle(field.Default);
                case PARAMDEF.DefType.f64: return Convert.ToDouble(field.Default);
                case PARAMDEF.DefType.fixstr: return "";
                case PARAMDEF.DefType.fixstrW: return "";
                case PARAMDEF.DefType.dummy8:
                    if (field.BitSize == -1)
                        return new byte[field.ArrayLength];
                    return (byte)0;

                default:
                    throw new NotImplementedException($"Default not implemented for type {field.DisplayType}");
            }
        }

        public static int GetBitLimit(PARAMDEF.DefType type)
        {
            if (type == PARAMDEF.DefType.u8)
                return 8;
            if (type == PARAMDEF.DefType.u16)
                return 16;
            if (type == PARAMDEF.DefType.u32)
                return 32;
            throw new InvalidOperationException("Bit type may only be u8, u16, or u32.");
        }
    }
}
