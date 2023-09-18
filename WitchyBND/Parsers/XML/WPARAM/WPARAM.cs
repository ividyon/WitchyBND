﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using PPlus;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;
using PARAM = WitchyFormats.PARAM;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM : WXMLParser
{
    public WBUtil.GameType? Game;

    private class WPARAMRow
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ParamdexName { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// The style in which cells will be read/written.
    /// CSV: Store all row cells as a CSV-style concatenated string with delimiters (min. readability).
    /// Attribute: Store all row cells as attributes on the row element. (readability compromise)
    /// Element: Store all row cells as separate elements (max. readability, max lines).
    /// </summary>
    private enum CellStyle
    {
        CSV,
        Attribute,
        Element
    }

    // Dictionary housing paramdefs for batched usage.
    private static Dictionary<WBUtil.GameType, Dictionary<string, PARAMDEF>> ParamdefStorage { get; } = new();

    // Dictionary housing param row names for batched usage.
    private static Dictionary<WBUtil.GameType, Dictionary<string, Dictionary<int, string>>>
        NameStorage { get; } = new();

    private static Dictionary<string, string> _ac6TentativeParamTypes;

    public override string Name => "PARAM";

    public override bool Is(string path)
    {
        return Path.GetExtension(path) == ".param";
    }

    public static void UnpackParamdex()
    {
        var paramdexPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex";
        var zipPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex.zip";
        if (File.Exists(zipPath))
        {
            PromptPlus.Error.WriteLine("");
            PromptPlus.Error.WriteLine("Located Paramdex archive; replacing existing Paramdex.");
            if (Directory.Exists(paramdexPath))
                Directory.Delete(paramdexPath, true);
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                if (!Directory.Exists(paramdexPath))
                    Directory.CreateDirectory(paramdexPath);
                PromptPlus.Error.WriteLine("Extracting Paramdex archive. This is a one-time operation.");
                archive.ExtractToDirectory(paramdexPath, true);
            }

            File.Delete(zipPath);
            PromptPlus.Error.WriteLine("Successfully extracted Paramdex archive.");
            PromptPlus.Error.WriteLine("");
        }
    }

    public static void PopulateParamdex(WBUtil.GameType game)
    {
        if (ParamdefStorage[game].Count > 0)
            return;

        UnpackParamdex();

        var paramdexPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex";
        if (!Directory.Exists(paramdexPath))
            throw new DirectoryNotFoundException("Could not locate Assets\\Paramdex folder.");

        // Populate tentative types
        var lines = File.ReadAllLines($@"{paramdexPath}\AC6\Defs\TentativeParamType.csv").ToList();
        lines.RemoveAt(0);
        _ac6TentativeParamTypes = lines.ToDictionary(a => a.Split(",")[0], b => b.Split(",")[1]);

        var gameName = game.ToString();
        var paramdefPath = $@"{paramdexPath}\{gameName}\Defs";

        if (!Directory.Exists(paramdefPath))
        {
            throw new Exception($"Paramdef path not found for {gameName}.");
        }

        // Reading XML paramdefs
        foreach (string path in Directory.GetFiles(paramdefPath, "*.xml"))
        {
            PARAMDEF paramdef = PARAMDEF.XmlDeserialize(path);

            var dupes = paramdef.Fields.GroupBy(x => x.InternalName).Where(x => x.Count() > 1)
                .ToDictionary(x => x.Key, x => x.ToList());
            foreach (KeyValuePair<string, List<PARAMDEF.Field>> pair in dupes)
            {
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    int offset = 0;
                    PARAMDEF.Field fieldDef = pair.Value[i];
                    var index = paramdef.Fields.IndexOf(fieldDef);
                    for (int j = 0; j < index; j++)
                    {
                        var prevDef = paramdef.Fields[j];
                        var type = WBUtil.TypeForParamDefType(prevDef.DisplayType, prevDef.ArrayLength > 1);
                        if (type == typeof(byte[]))
                        {
                            offset += prevDef.ArrayLength;
                        }
                        else
                            offset += Marshal.SizeOf(type);
                    }

                    fieldDef.InternalName += $"_offset{offset}";
                }
            }

            ParamdefStorage[game][paramdef.ParamType] = paramdef;
        }
    }

    public static void PopulateNames(WBUtil.GameType game, string paramName)
    {
        if (NameStorage[game].ContainsKey(paramName) && NameStorage[game][paramName].Count > 0)
            return;

        var gameName = game.ToString();
        var namePath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\Names\{paramName}.txt";

        if (!File.Exists(namePath))
        {
            // Write something to the storage so the population process isn't repeated.
            NameStorage[game][paramName] = new Dictionary<int, string>()
            {
                { -9000, string.Empty }
            };
            // Quietly fail, it's just names after all.
            Program.RegisterNotice($"Could not find names for {gameName} param {paramName} in Paramdex.");
            return;
        }

        var nameDict = new Dictionary<int, string>();
        var names = File.ReadAllLines(namePath);
        foreach (string name in names)
        {
            var splitted = name.Split(' ', 2);
            try
            {
                var result = nameDict.TryAdd(int.Parse(splitted[0]), splitted[1]);
                if (result == false)
                {
                    Program.RegisterNotice($"Paramdex: Duplicate name for ID {splitted[0]}");
                }
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"There was something wrong with the Paramdex names at \"{name}\"",
                    e);
            }
        }

        NameStorage[game][paramName] = nameDict;
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

    public static object StringToCellValue(string valueString, PARAMDEF.Field pdefField)
    {
        object value;
        if (pdefField.DisplayType == PARAMDEF.DefType.dummy8)
        {
            var bytes = valueString.Substring(0, valueString.Length - 1).Substring(1).Split('|')
                .Select(byteString => Convert.ToByte(byteString));

            if (pdefField.BitSize == -1)
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
            value = valueString;
        }

        return value;
    }

    public static object ConvertValueFromString(PARAMDEF.Field def, object value)
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

    public WPARAM()
    {
        foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
        {
            ParamdefStorage[game] = new Dictionary<string, PARAMDEF>();
            NameStorage[game] = new Dictionary<string, Dictionary<int, string>>();
        }
    }
}