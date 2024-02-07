using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PPlus;
using PPlus.Controls;
using WitchyBND.Errors;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Services;

public interface IGameService
{
    ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, PARAMDEF>> ParamdefStorage { get; }

    ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, ConcurrentDictionary<int, string>>> NameStorage
    {
        get;
    }

    Dictionary<string, string> Ac6TentativeParamTypes { get; }
    Dictionary<string, (WBUtil.GameType, ulong)> KnownGamePathsForParams { get; }
    Dictionary<string, WBUtil.GameType> KnownGamePaths { get; }
    Dictionary<string, WBUtil.GameType> KnownExecutables { get; }

    (WBUtil.GameType, ulong) DetermineGameType(string path, bool forParams, WBUtil.GameType? game = null,
        ulong regVer = 0);

    void PopulateParamdex(WBUtil.GameType game);
    void PopulateNames(WBUtil.GameType game, string paramName);
}

public class GameService : IGameService
{
    private IErrorService _errorService;

    // Dictionary housing paramdefs for batched usage.
    public ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, PARAMDEF>> ParamdefStorage { get; } =
        new();

    // Dictionary housing param row names for batched usage.
    public ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, ConcurrentDictionary<int, string>>>
        NameStorage { get; } = new();

    public Dictionary<string, string> Ac6TentativeParamTypes { get; } = new();
    public Dictionary<string, (WBUtil.GameType, ulong)> KnownGamePathsForParams { get; } = new();
    public Dictionary<string, WBUtil.GameType> KnownGamePaths { get; } = new();

    public Dictionary<string, WBUtil.GameType> KnownExecutables { get; } = new()
    {
        { "eldenring.exe", WBUtil.GameType.ER },
        { "DarkSoulsIII.exe", WBUtil.GameType.DS3 },
        { "DarkSoulsII.exe", WBUtil.GameType.DS2S },
        { "armoredcore.exe", WBUtil.GameType.AC6 },
        { "sekiro.exe", WBUtil.GameType.SDT },
    };

    public WBUtil.GameType? DsmsGameTypeToWitchyGameType(DsmsGameType type)
    {
        return type switch
        {
            DsmsGameType.DemonsSouls => WBUtil.GameType.DES,
            DsmsGameType.DarkSoulsPTDE => WBUtil.GameType.DS1,
            DsmsGameType.DarkSoulsRemastered => WBUtil.GameType.DS1R,
            DsmsGameType.DarkSoulsIISOTFS => WBUtil.GameType.DS2S,
            DsmsGameType.DarkSoulsIII => WBUtil.GameType.DS3,
            DsmsGameType.Bloodborne => WBUtil.GameType.BB,
            DsmsGameType.Sekiro => WBUtil.GameType.SDT,
            DsmsGameType.EldenRing => WBUtil.GameType.ER,
            DsmsGameType.ArmoredCoreVI => WBUtil.GameType.AC6,
            _ => null
        };
    }

    public GameService(IErrorService errorService)
    {
        _errorService = errorService;
        UnpackParamdex();
        PopulateTentativeAC6Types();

        foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
        {
            ParamdefStorage[game] = new();
            NameStorage[game] = new();
        }
    }

    public void PopulateTentativeAC6Types()
    {
        var tentativeTypePath = $@"{WBUtil.GetParamdexPath()}\AC6\Defs\TentativeParamType.csv";

        if (File.Exists(tentativeTypePath))
        {
            var lines = File.ReadAllLines(tentativeTypePath).ToList();
            lines.RemoveAt(0);
            foreach (string line in lines)
            {
                var split = line.Split(",");
                Ac6TentativeParamTypes[split[0]] = split[1];
            }
        }
    }

    public (WBUtil.GameType, ulong) DetermineGameType(string path, bool forParams,
        WBUtil.GameType? game = null, ulong regVer = 0)
    {
        string knownPath = File.Exists(path) ? Path.GetDirectoryName(path)! : path;

        // Determine what kind of PARAM we're dealing with here
        if (forParams)
        {
            var known = KnownGamePathsForParams.Keys.FirstOrDefault(p => path.StartsWith(p));
            if (known != null) return KnownGamePathsForParams[known];

            string? xmlPath = WBUtil.TraverseFindFile("_witchy-bnd4.xml", path);
            if (xmlPath != null)
            {
                XDocument xDoc = XDocument.Load(xmlPath);

                if (xDoc.Root?.Element("game")?.Value != null)
                {
                    Enum.TryParse(xDoc.Root!.Element("game")!.Value, out WBUtil.GameType regGame);
                    game = regGame;
                }

                if (xDoc.Root?.Element("version")?.Value != null)
                {
                    regVer = Convert.ToUInt64(xDoc.Root!.Element("version")!.Value ?? "0");
                }

                knownPath = Path.GetDirectoryName(xmlPath);
            }
        }
        else
        {
            var known = KnownGamePaths.Keys.FirstOrDefault(p => path.StartsWith(p));
            if (known != null) return (KnownGamePaths[known], regVer);
        }

        // Attempt to find game by project.json
        if (game == null)
        {
            string? pJsonPath = WBUtil.TraverseFindFile("project.json", path);
            if (pJsonPath != null)
            {
                try
                {
                    var json = File.ReadAllLines(pJsonPath);
                    var type = json.Select(s =>
                        Regex.Match(s, @"\s*?""GameType"":\s(.*?),")).FirstOrDefault(s => s.Success);
                    var dsmsGameType = Enum.Parse<DsmsGameType>(type.Groups[1].Value);
                    game = DsmsGameTypeToWitchyGameType(dsmsGameType);
                    knownPath = Path.GetDirectoryName(pJsonPath);
                }
                catch (Exception e)
                {
                    lock (Program.ConsoleWriterLock)
                        PromptPlus.Error.WriteLine($"Could not read DSMS project file at {pJsonPath}.");
                }
            }
        }

        // Attempt to find game by EXE
        if (game == null)
        {
            foreach ((string exeName, WBUtil.GameType exeGame) in KnownExecutables)
            {
                var traversePath = WBUtil.TraverseFindFile(exeName, path);
                if (traversePath != null)
                {
                    game = exeGame;
                    knownPath = Path.GetDirectoryName(traversePath)!;
                }
            }
        }

        if (game != null)
        {
            lock (Program.ConsoleWriterLock)
                PromptPlus.WriteLine($"Determined game for Paramdex: {game.Value.ToString()}".PromptPlusEscape());
        }
        else
        {
            lock (Program.ConsoleWriterLock)
                PromptPlus.Error.WriteLine("Could not determine param game version.");
            if (!Configuration.Args.Passive)
            {
                lock (Program.ConsoleWriterLock)
                {
                    var select = PromptPlus
                        .Select<WBUtil.GameType>("Please select the Paramdex of one of the following games")
                        .Run();
                    if (select.IsAborted)
                    {
                        throw new Exception("Could not determine PARAM type.");
                    }

                    game = select.Value;
                    knownPath = Path.GetDirectoryName(path);
                }
            }
            else
            {
                throw new Exception("Could not determine PARAM type.");
            }
        }

        if (forParams && regVer == 0)
        {
            lock (Program.ConsoleWriterLock)
                PromptPlus.Error.WriteLine("Could not determine regulation version.");
            if (!Configuration.Args.Passive)
            {
                lock (Program.ConsoleWriterLock)
                {
                    PromptPlus.WriteLine(@"Please input the regulation version to use for reading the PARAM.
Format examples:
    ""10210005"" for Armored Core VI 1.02.1
    ""11001000"" for Elden Ring 1.10.1
Enter 0, or press ESC, to use the latest available paramdef.");
                    var input = PromptPlus.Input("Input regulation version")
                        .AddValidators(PromptValidators.IsTypeULong())
                        .ValidateOnDemand()
                        .Run();
                    if (input.IsAborted)
                    {
                        PromptPlus.Error.WriteLine("Defaulting to latest paramdef.");
                    }
                    else
                    {
                        regVer = Convert.ToUInt64(input.Value);
                    }
                }
            }
            else
            {
                lock (Program.ConsoleWriterLock)
                    PromptPlus.Error.WriteLine("Defaulting to latest paramdef.");
            }
        }

        KnownGamePaths[knownPath] = game.Value;
        if (forParams)
        {
            KnownGamePathsForParams[knownPath] = (game.Value, regVer);
            PopulateParamdex(game.Value);
        }

        return (game.Value, regVer);
    }

    public void UnpackParamdex()
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
            _errorService.RegisterError(new WitchyError(
                @"A problem occurred while extracting the Paramdex archive. Please extract it manually in the ""Assets"" directory in the WitchyBND folder. Alternately, try re-downloading WitchyBND, as the download may have been corrupted."));
        }
    }

    public void PopulateParamdex(WBUtil.GameType game)
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

    public void PopulateNames(WBUtil.GameType game, string paramName)
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
                    _errorService.RegisterNotice($"Paramdex: Duplicate name for ID {splitted[0]}");
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
}