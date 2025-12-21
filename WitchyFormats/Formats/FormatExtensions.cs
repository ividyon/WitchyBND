using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SoulsFormats;
namespace WitchyFormats;

public static class FormatExtensions
{
    /// <summary>
    /// Converts a value to a string based on this ParameterTemplate's type.
    /// </summary>
    public static string ValueToStorage(this TAE.Template.ParameterTemplate template, object val)
    {
        if (template.EnumEntries != null)
        {
            if (template.EnumEntries.Values.Contains(val))
            {
                return val.ToString();
            }
        }

        switch (template.Type)
        {
            case TAE.Template.ParamType.aob: return string.Join(" ", ((byte[])val).Select(b => b.ToString("X2")));
            case TAE.Template.ParamType.x8: return ((byte)val).ToString("X2");
            case TAE.Template.ParamType.x16: return ((ushort)val).ToString("X4");
            case TAE.Template.ParamType.x32: return ((uint)val).ToString("X8");
            case TAE.Template.ParamType.x64: return ((ulong)val).ToString("X16");
            case TAE.Template.ParamType.b: return ((bool)val) ? "True" : "False";
            case TAE.Template.ParamType.f32grad:
                return $"{((System.Numerics.Vector2)val).X}|{((System.Numerics.Vector2)val).Y}";
            default: return val.ToString();
        }
    }

    /// <summary>
    /// Converts a string to a value based on this ParameterTemplate's type.
    /// </summary>
    public static object StorageToValue(this TAE.Template.ParameterTemplate template, string str)
    {
        if (str == null)
            return null;

        IEnumerable<string> GetArrayFromSingleLineString(string s)
        {
            return s.Split(' ')
                .Where(st => !string.IsNullOrWhiteSpace(st))
                .Select(st => st.Trim());
        }

        List<string> GetArrayFromString(string s)
        {
            List<string> result = new List<string>();
            var lines = s.Split('\n');
            foreach (var l in lines)
                result.AddRange(GetArrayFromSingleLineString(l.Replace("\r", "").Replace("\n", "").Replace("\t", "")));
            return result;
        }

        switch (template.Type)
        {
            case TAE.Template.ParamType.aob:
                return GetArrayFromString(str).Select(b => byte.Parse(b, System.Globalization.NumberStyles.HexNumber))
                    .ToArray();
            case TAE.Template.ParamType.u8: return byte.Parse(str);
            case TAE.Template.ParamType.x8: return byte.Parse(str, System.Globalization.NumberStyles.HexNumber);
            case TAE.Template.ParamType.s8: return sbyte.Parse(str);
            case TAE.Template.ParamType.u16: return ushort.Parse(str);
            case TAE.Template.ParamType.x16: return ushort.Parse(str, System.Globalization.NumberStyles.HexNumber);
            case TAE.Template.ParamType.s16: return short.Parse(str);
            case TAE.Template.ParamType.u32: return uint.Parse(str);
            case TAE.Template.ParamType.x32: return uint.Parse(str, System.Globalization.NumberStyles.HexNumber);
            case TAE.Template.ParamType.s32: return int.Parse(str);
            case TAE.Template.ParamType.u64: return ulong.Parse(str);
            case TAE.Template.ParamType.x64: return ulong.Parse(str, System.Globalization.NumberStyles.HexNumber);
            case TAE.Template.ParamType.s64: return long.Parse(str);
            case TAE.Template.ParamType.f32: return float.Parse(str);
            case TAE.Template.ParamType.f32grad:
                var floatSplit = str.Split('|');
                float gradStart = float.Parse(floatSplit[0]);
                float gradEnd = float.Parse(floatSplit[1]);
                return new System.Numerics.Vector2(gradStart, gradEnd);
            case TAE.Template.ParamType.f64: return double.Parse(str);
            case TAE.Template.ParamType.b:
                string toLower = str.ToLower().Trim();
                if (toLower == "true")
                    return true;
                else if (toLower == "false")
                    return false;
                else
                    throw new FormatException("Boolean value must be either 'True' or 'False', case-insensitive.");
            default: throw new Exception($"Invalid ParamTemplate ParamType: {template.Type.ToString()}");
        }
    }
}