using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using PPlus;
using PPlus.Controls;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;
using PARAM = WitchyFormats.PARAM;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM : WXMLParser
{
    public static Dictionary<string, (WBUtil.GameType, ulong)?> Games = new();
    private static bool WarnedAboutParams { get; set; }

    public static bool WarnAboutParams()
    {
        if (Configuration.Expert || WarnedAboutParams || Configuration.Args.Passive) return true;

        PromptPlus.WriteLine("");
        List<string> lines = new()
        {
            "[RED]Editing PARAMs using WitchyBND is highly discouraged.[/]",
            @"For PARAM editing, DSMapStudio is the recommended application. It can be downloaded from GitHub.
If DSMapStudio does not yet support this game or regulation version, an experimental build may be available at ?ServerName? Discord.",
            "Editing, merging and upgrading of new PARAM entries should all be done in DSMapStudio.",
            "Merging outdated PARAMs (from a previous regulation) with WitchyBND is guaranteed to cause issues.",
            "WitchyBND is not capable of upgrading outdated PARAMs to a newer regulation version under ANY circumstances!",
        };

        foreach (string line in lines)
        {
            PromptPlus.WriteLine(line);
            var cursor = PromptPlus.GetCursorPosition();
            PromptPlus.WriteLine("");
            PromptPlus.WaitTimer("Please read carefully, then press any key...", TimeSpan.FromSeconds(1));
            PromptPlus.ClearLine();
            PromptPlus.SetCursorPosition(cursor.Left, cursor.Top);
            PromptPlus.WriteLine("");
            PromptPlus.KeyPress("Please read carefully, then press any key...").Run();
            PromptPlus.ClearLine();
            PromptPlus.SetCursorPosition(cursor.Left, cursor.Top);
        }

        PromptPlus.WriteLine("");
        var confirm = PromptPlus.Confirm(@"Do you still wish to proceed?").Run();

        if (confirm.Value.IsNoResponseKey() || confirm.IsAborted)
            return false;

        WarnedAboutParams = true;
        return true;
    }

    private class WPARAMRow
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ParamdexName { get; set; }
        public Dictionary<string, string> Fields { get; } = new();
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
    private static ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, PARAMDEF>> ParamdefStorage
    {
        get;
    } = new();

    // Dictionary housing param row names for batched usage.
    private static
        ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, ConcurrentDictionary<int, string>>>
        NameStorage { get; } = new();

    private static Dictionary<string, string> _ac6TentativeParamTypes = new();

    public override string Name => "PARAM";

    public override bool HasPreprocess => true;

    public static string GetGamePath(string path)
    {
        string dirPath = Path.GetDirectoryName(path)!;
        string? xmlPath = WBUtil.TraverseFindFile("_witchy-bnd4.xml", path);

        return xmlPath != null ? Path.GetDirectoryName(xmlPath)! : dirPath;
    }
    public override bool Preprocess(string srcPath)
    {
        string gamePath = GetGamePath(srcPath);
        if (PreprocessedPaths.Contains(gamePath)) return false;

        if (!Is(srcPath, null, out ISoulsFile? _)) return false;

        (WBUtil.GameType, ulong)? gameInfo;
        gameInfo = Games.TryGetValue(gamePath, out gameInfo) ? gameInfo : WBUtil.DetermineParamdexGame(gamePath, Configuration.Args.Passive);

        Games[gamePath] = gameInfo;

        if (Games[gamePath] == null)
            throw new InvalidDataException("Could not locate game type of PARAM.");

        PopulateParamdex(Games[gamePath]!.Value.Item1);

        PreprocessedPaths.Add(gamePath);
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
            Program.RegisterError($"{path} is not a valid PARAM file.");
            file = null;
            return false;
        }

        file = param;
        return true;
    }

    public static void UnpackParamdex()
    {
        var paramdexPath = WBUtil.GetParamdexPath();
        var zipPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex.zip";

        if (!File.Exists(zipPath)) return;

        PromptPlus.Error.WriteLine("");
        PromptPlus.Error.WriteLine("Located Paramdex archive; replacing existing Paramdex.");
        if (Directory.Exists(paramdexPath))
            Directory.Delete(paramdexPath, true);
        try
        {
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
        catch (Exception e)
        {
            Program.RegisterError(new WitchyError(@"A problem occurred while extracting the Paramdex archive. Please extract it manually in the ""Assets"" directory in the WitchyBND folder. Alternately, try re-downloading WitchyBND, as the download may have been corrupted."));
        }
    }

    public static void PopulateParamdex(WBUtil.GameType game)
    {
        if (ParamdefStorage[game].Count > 0)
            return;

        var paramdexPath = WBUtil.GetParamdexPath();
        if (!Directory.Exists(paramdexPath))
            throw new DirectoryNotFoundException("Could not locate Assets\\Paramdex folder.");

        var gameName = game.ToString();
        var paramdefPath = $@"{paramdexPath}\{gameName}\Defs";

        if (!Directory.Exists(paramdefPath))
        {
            throw new Exception($"Paramdef path not found for {gameName}.");
        }

        // Reading XML paramdefs
        foreach (string path in Directory.GetFiles(paramdefPath, "*.xml"))
        {
            PARAMDEF paramdef = PARAMDEF.XmlDeserialize(path, true);

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
        var namePath = Path.Combine(WBUtil.GetParamdexPath($@"{gameName}\Names\{paramName}.txt"));

        if (!File.Exists(namePath))
        {
            // Write something to the storage so the population process isn't repeated.
            NameStorage[game][paramName] = new ConcurrentDictionary<int, string>();
            NameStorage[game][paramName].TryAdd(-9000, string.Empty);
            // Quietly fail, it's just names after all.
            // Program.RegisterNotice($"Could not find names for {gameName} param {paramName} in Paramdex.");
            return;
        }

        var nameDict = new ConcurrentDictionary<int, string>();
        var names = File.ReadAllLines(namePath);
        foreach (string name in names)
        {
            var splitted = name.Trim().Split(' ', 2);
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
        UnpackParamdex();

        // Populate AC6 tentative types
        var tentativeTypePath = $@"{WBUtil.GetParamdexPath()}\AC6\Defs\TentativeParamType.csv";

        if (File.Exists(tentativeTypePath))
        {
            var lines = File.ReadAllLines($@"{WBUtil.GetParamdexPath()}\AC6\Defs\TentativeParamType.csv").ToList();
            lines.RemoveAt(0);
            _ac6TentativeParamTypes = lines.ToDictionary(a => a.Split(",")[0], b => b.Split(",")[1]);
        }

        foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
        {
            ParamdefStorage[game] = new();
            NameStorage[game] = new();
        }
    }
}