using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulsFormats;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;
public partial class WTAEFolder : WFolderParser
{
    public override string Name => "TAE (Folder)";
    public override string XmlTag => "tae";
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;

    public override bool HasPreprocess => true;
    public override bool Preprocess(string srcPath, bool recursive, ref Dictionary<string, (WFileParser, ISoulsFile)> files)
    {
        ISoulsFile? file = null;
        if (!((recursive || ExistsUnpacked(srcPath)) && IsUnpacked(srcPath)) &&
            !((recursive || Exists(srcPath)) && IsSimpleFirst(srcPath, null, out file)))
        {
            return false;
        }
        gameService.DetermineGameType(srcPath, IGameService.GameDeterminationType.Other);
        gameService.PopulateTAETemplates();
        if (file != null)
            files.TryAdd(srcPath, (this, file));

        return true;
    }

    private static bool WarnedAboutTAEs { get; set; }

    public static bool WarnAboutTAEs()
    {
        if (Configuration.Active.Expert || WarnedAboutTAEs || Configuration.Active.Passive) return true;

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