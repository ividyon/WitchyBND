using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PPlus.Controls;
using SoulsFormats.Formats.TAE;
using WitchyBND.Errors;
using WitchyBND.Parsers;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Services;

public interface IGameService
{

    public enum GameDeterminationType
    {
        PARAM,
        PARAMBND,
        Other
    }
    ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, PARAMDEF>> ParamdefStorage { get; }

    ConcurrentDictionary<WBUtil.GameType, ConcurrentDictionary<string, ConcurrentDictionary<int, string>>> NameStorage
    {
        get;
    }

    Dictionary<string, string> Ac6TentativeParamTypes { get; }
    Dictionary<string, (WBUtil.GameType, ulong)> KnownGamePathsForParams { get; }
    Dictionary<string, WBUtil.GameType> KnownGamePaths { get; }
    Dictionary<string, WBUtil.GameType> KnownExecutables { get; }

    (WBUtil.GameType, ulong) DetermineGameType(string path, GameDeterminationType type, WBUtil.GameType? game = null,
        ulong regVer = 0);

    void PopulateParamdex(WBUtil.GameType game);
    void PopulateTAETemplates();
    TAE.Template GetTAETemplate(WBUtil.GameType game);
    void UnpackParamdex();
    void PopulateNames(WBUtil.GameType game, string paramName);
}

public class GameService : IGameService
{
    private IErrorService _errorService;
    private IOutputService output;

    public GameService(IErrorService errorService, IOutputService outputService)
    {
        _errorService = errorService;
        output = outputService;

        foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
        {
            ParamdefStorage[game] = new();
            NameStorage[game] = new();
        }
    }

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
        { "nightreign.exe", WBUtil.GameType.NR }
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
            DsmsGameType.Nightreign => WBUtil.GameType.NR,
            _ => null
        };
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
                if (string.IsNullOrWhiteSpace(line)) continue;
                var split = line.Split(",");
                Ac6TentativeParamTypes[split[0]] = split[1];
            }
        }
    }
    public (WBUtil.GameType, ulong) DetermineGameType(string path, IGameService.GameDeterminationType type,
        WBUtil.GameType? game = null, ulong regVer = 0)
    {
        UnpackParamdex();
        string knownPath = File.Exists(path) ? Path.GetDirectoryName(path)! : path;

        // Determine what kind of PARAM we're dealing with here
        if (type == IGameService.GameDeterminationType.PARAM || type == IGameService.GameDeterminationType.PARAMBND)
        {
            var known = KnownGamePathsForParams.Keys.FirstOrDefault(p => path.StartsWith(p));
            if (known != null) return KnownGamePathsForParams[known];

            string? xmlPath = WBUtil.TraverseFindFile("_witchy-bnd4.xml", path);
            if (xmlPath != null)
            {
                XDocument xDoc = XDocument.Load(xmlPath);

                if (game == null && xDoc.Root != null)
                {
                    game = WFileParser.GetGameTypeFromXml(xDoc.Root);
                }

                if (regVer == 0 && xDoc.Root?.Element("version")?.Value != null)
                {
                    try
                    {
                        regVer = Convert.ToUInt64(xDoc.Root!.Element("version")!.Value ?? "0");
                    }
                    catch
                    {
                        regVer = 0;
                    }
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
                    var gameType = json.Select(s =>
                        Regex.Match(s, @"\s*?""GameType"":\s(.*?),")).FirstOrDefault(s => s.Success);
                    var dsmsGameType = Enum.Parse<DsmsGameType>(gameType.Groups[1].Value);
                    game = DsmsGameTypeToWitchyGameType(dsmsGameType);
                    knownPath = Path.GetDirectoryName(pJsonPath);
                }
                catch (Exception e)
                {
                    output.WriteError($"Could not read DSMS project file at {pJsonPath}.");
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
            output.WriteLine($"Determined game for Paramdex: {game.Value.ToString()}".PromptPlusEscape());
        }
        else
        {
            output.WriteError("Could not determine param game version.");
            if (!Configuration.Active.Passive)
            {
                var select = output
                    .Select<WBUtil.GameType>("Please select the Paramdex of one of the following games")
                    .Run();
                if (select.IsAborted)
                {
                    throw new Exception("Could not determine PARAM type.");
                }

                game = select.Value;
                knownPath = Path.GetDirectoryName(path);
            }
            else
            {
                throw new Exception("Could not determine PARAM type.");
            }
        }

        if (regVer == 0)
        {
            switch (type)
            {
                case IGameService.GameDeterminationType.PARAM:
                    output.WriteError("Could not determine regulation version.");
                    if (!Configuration.Active.Passive)
                    {
                        output.WriteLine(@"Please input the regulation version to use for reading the PARAM.
Format examples:
""10210005"" for Armored Core VI 1.02.1
""11001000"" for Elden Ring 1.10.1
Enter 0, or leave it empty, to use the latest available paramdef.");
                        var input = output.Input("Input regulation version")
                            .AddValidators(PromptValidators.IsTypeULong())
                            .ValidateOnDemand()
                            .DefaultIfEmpty("0")
                            .Config(config => {
                                config.EnabledAbortKey(false);
                            })
                            .Run();
                        if (input.IsAborted || input.Value == "0")
                        {
                            output.WriteError("Defaulting to latest paramdef.");
                        }
                        else
                        {
                            regVer = Convert.ToUInt64(input.Value);
                        }
                    }
                    else
                    {
                        output.WriteError("Defaulting to latest paramdef.");
                    }
                    break;
                // case IGameService.GameDeterminationType.PARAMBND:
                //
                //     break;
            }
        }

        KnownGamePaths[knownPath] = game.Value;
        if (game.Value == WBUtil.GameType.AC6)
        {
            PopulateTentativeAC6Types();
        }
        if (type == IGameService.GameDeterminationType.PARAM || type == IGameService.GameDeterminationType.PARAMBND)
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

        output.WriteError("");
        output.WriteError("Located Paramdex archive; replacing existing Paramdex.");
        if (Directory.Exists(paramdexPath))
            Directory.Delete(paramdexPath, true);
        try
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                if (!Directory.Exists(paramdexPath))
                    Directory.CreateDirectory(paramdexPath);
                output.WriteError("Extracting Paramdex archive. This is a one-time operation.");
                archive.ExtractToDirectory(paramdexPath, true);
            }

            File.Delete(zipPath);
            output.WriteError("Successfully extracted Paramdex archive.");
            output.WriteError("");
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
            _errorService.RegisterError($"Paramdef path not found for {gameName}. Errors may occur.");
            return;
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

    private static readonly Dictionary<WBUtil.GameType, TAE.Template> templateDict = new();
    public void PopulateTAETemplates()
    {
        if (templateDict.Any()) return;
        foreach (var type in Enum.GetValues<WBUtil.GameType>().Except(new [] { WBUtil.GameType.AC6 }))
        {
            var path = WBUtil.GetAssetsPath("Templates", $"TAE.Template.{type}.xml");
            if (File.Exists(path))
                templateDict[type] = TAE.Template.ReadXMLFile(path);
        }
    }

    public TAE.Template GetTAETemplate(WBUtil.GameType game)
    {
        if (templateDict.ContainsKey(game)) return templateDict[game];
        PopulateTAETemplates();
        if (!templateDict.ContainsKey(game))
            throw new GameUnsupportedException(game);
        return templateDict[game];
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
            if (splitted.Length != 2)
                continue;
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
                throw new InvalidDataException($"There was something wrong with the Paramdex names at \"{name}\" in {Path.GetFileNameWithoutExtension(namePath)}",
                    e);
            }
        }

        NameStorage[game][paramName] = nameDict;
    }
}