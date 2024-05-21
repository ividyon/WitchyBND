using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using PPlus;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM : WXMLParser
{

    private static bool WarnedAboutParams { get; set; }

    public static bool WarnAboutParams()
    {
        if (Configuration.Expert || WarnedAboutParams || Configuration.Args.Passive) return true;

        List<string> lines = new()
        {
            "[RED]Editing PARAMs using WitchyBND is highly discouraged.[/]",
            @"For PARAM editing, DSMapStudio is the recommended application. It can be downloaded from GitHub.
If DSMapStudio does not yet support this game or regulation version, an experimental build may be available at ?ServerName? Discord.",
            "Editing, merging and upgrading of new PARAM entries should all be done in DSMapStudio.",
            "Merging outdated PARAMs (from a previous regulation) with WitchyBND is guaranteed to cause issues.",
            "WitchyBND is not capable of upgrading outdated PARAMs to a newer regulation version under ANY circumstances!",
        };
        var warned = WBUtil.ObnoxiousWarning(lines);
        WarnedAboutParams = warned;
        return warned;
    }

    private class WPARAMRow
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? ParamdexName { get; set; }
        public Dictionary<string, string> Fields { get; } = new();
    }

    /// <summary>
    /// The style in which cells will be read/written.
    /// CSV: Store all row cells as a CSV-style concatenated string with delimiters (min. readability).
    /// Attribute: Store all row cells as attributes on the row element. (readability compromise)
    /// Element: Store all row cells as separate elements (max. readability, max lines).
    /// </summary>
    public enum CellStyle
    {
        [Display(Name = "XML attribute",
            Description = @"Each field is added to the row element as an attribute. Small file size.")]
        Attribute,
        [Display(Name = "XML element",
            Description = @"Each field is added to the row element as a child element. Easier to tell differences between two versions of the file.")]
        Element,
        [Display(Name = "CSV",
            Description = @"Stores all fields as a single delimited string. The least readable, potentially smallest file size.")]
        CSV
    }

    public override string Name => "PARAM";
    public override int Version => WBUtil.WitchyVersionToInt("2.7.1.0");

    public override bool HasPreprocess => true;

    public override bool Preprocess(string srcPath)
    {
        if (gameService.KnownGamePathsForParams.Any(p => srcPath.StartsWith(p.Key))) return false;

        if (!Is(srcPath, null, out ISoulsFile? _)) return false;

        gameService.DetermineGameType(srcPath, true);

        return true;
    }

    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        if (Path.GetExtension(path) != ".param")
        {
            file = null;
            return false;
        }

        FsParam? param;
        try
        {
            param = FsParam.Read(path);
        }
        catch
        {
            errorService.RegisterError($"{path} is not a valid PARAM file.");
            file = null;
            return false;
        }

        file = param;
        return true;
    }

    public static Byte[] Dummy8Read(string dummy8, int expectedLength)
    {
        Byte[] nval = new Byte[expectedLength];
        if (!(dummy8.StartsWith('[') && dummy8.EndsWith(']')))
            return null;
        string[] spl = dummy8.Substring(1, dummy8.Length - 2).Split('|');
        if (nval.Length != spl.Length)
        {
            return null;
        }

        for (int i = 0; i < nval.Length; i++)
        {
            if (!byte.TryParse(spl[i], out nval[i]))
                return null;
        }

        return nval;
    }

    public static string CellValueToString(FsParam.Cell cell)
    {
        var value = cell.Value.ToString();
        if (cell.Def.DisplayType == PARAMDEF.DefType.dummy8)
        {
            byte[] bytes;
            if (cell.Value.GetType() == typeof(byte))
            {
                bytes = new[] { (byte)cell.Value };
            }
            else
            {
                bytes = (byte[])cell.Value;
            }

            value = $"[{String.Join("|", bytes.Select(myByte => myByte.ToString()))}]";
        }

        return value;
    }

    public static object StringToCellValue(PARAMDEF.Field def, string valueString)
    {
        object value;
        if (def.DisplayType == PARAMDEF.DefType.dummy8)
        {
            var bytes = valueString.Substring(0, valueString.Length - 1).Substring(1).Split('|')
                .Select(byteString => Convert.ToByte(byteString));

            if (def.BitSize == -1)
            {
                value = bytes.ToArray();
                if (bytes.Count() == 1)
                    value = bytes.First();
            }
            else
            {
                value = bytes.First();
            }
        }
        else
        {
            value = StringToValue(def, valueString);
        }

        return value;
    }

    public static object StringToValue(PARAMDEF.Field def, object value)
    {
        if (value == null)
            throw new NullReferenceException($"Cell value may not be null.");

        switch (def.DisplayType)
        {
            case PARAMDEF.DefType.s8: return Convert.ToSByte(value);
            case PARAMDEF.DefType.u8: return Convert.ToByte(value);
            case PARAMDEF.DefType.s16: return Convert.ToInt16(value);
            case PARAMDEF.DefType.u16: return Convert.ToUInt16(value);
            case PARAMDEF.DefType.s32: return Convert.ToInt32(value);
            case PARAMDEF.DefType.u32: return Convert.ToUInt32(value);
            case PARAMDEF.DefType.b32: return Convert.ToInt32(value);
            case PARAMDEF.DefType.f32: return Convert.ToSingle(value);
            case PARAMDEF.DefType.angle32: return Convert.ToSingle(value);
            case PARAMDEF.DefType.f64: return Convert.ToDouble(value);
            case PARAMDEF.DefType.fixstr: return Convert.ToString(value);
            case PARAMDEF.DefType.fixstrW: return Convert.ToString(value);
            case PARAMDEF.DefType.dummy8:
                if (def.ArrayLength > 1)
                    return (byte[])value;
                return Convert.ToByte(value);

            default:
                throw new NotImplementedException($"Conversion not specified for type {def.DisplayType}");
        }
    }
}