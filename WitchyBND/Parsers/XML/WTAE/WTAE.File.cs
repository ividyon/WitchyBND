using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;
public partial class WTAEFile : WXMLParser
{
    public override string Name => "TAE (File)";
    public override string XmlTag => "taeFile";
    public override bool HasPreprocess => true;
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;

    private static readonly Dictionary<WBUtil.GameType, TAE.Template> templateDict = new();
    public override bool Preprocess(string srcPath)
    {
        if (!(ExistsUnpacked(srcPath) && IsUnpacked(srcPath)) && !(Exists(srcPath) && Is(srcPath, null, out ISoulsFile? _))) return false;
        gameService.DetermineGameType(srcPath, false);
        if (templateDict.Any()) return false;
        foreach (var type in Enum.GetValues<WBUtil.GameType>().Except(new [] { WBUtil.GameType.AC6 }))
        {
            var path = WBUtil.GetParamdexPath(type.ToString(), $"TAE.Template.{type}.xml");
            if (File.Exists(path))
                templateDict[type] = TAE.Template.ReadXMLFile(path);
        }
        return false; // Preprocess them all to perform WarnAboutTAEs
    }

    private static WBUtil.GameType FormatToGame(TAE.TAEFormat format)
    {
        switch (format)
        {
            case TAE.TAEFormat.DS1:
                return WBUtil.GameType.DS1;
            case TAE.TAEFormat.SOTFS:
                return WBUtil.GameType.DS2;
            case TAE.TAEFormat.DS3:
                return WBUtil.GameType.DS3;
            case TAE.TAEFormat.SDT:
                return WBUtil.GameType.SDT;
            case TAE.TAEFormat.DES:
                return WBUtil.GameType.DES;
            case TAE.TAEFormat.DESR:
                return WBUtil.GameType.DES;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static bool WarnedAboutTAEs { get; set; }

    public static bool WarnAboutTAEs()
    {
        if (Configuration.Expert || WarnedAboutTAEs || Configuration.Args.Passive) return true;

        List<string> lines = new()
        {
            "[RED]Editing TAEs using WitchyBND is highly discouraged.[/]",
            @"For TAE editing, DS Anim Studio is the recommended application. It can be downloaded from Meowmaritus' Patreon.
If DS Anim Studio does not yet support this game, an experimental build may be available.",
            "Editing TAEs with WitchyBND is likely to cause issues.",
            "TAE support is mainly intended for comparisons and version control!",
        };

        var warned = WBUtil.ObnoxiousWarning(lines);
        WarnedAboutTAEs = warned;
        return warned;
    }
}