using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;
public partial class WMSBEFolder : WFolderParser
{
    public override string Name => "MSBE (Folder)";
    public override string XmlTag => "msbe";
    public override bool HasPreprocess => true;
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;
    public override bool Preprocess(string srcPath)
    {
        return false; // Preprocess them all to perform WarnAboutTAEs
    }
    private static bool WarnedAboutMSBs { get; set; }

    public static bool WarnAboutMSBs()
    {
        if (Configuration.Expert || WarnedAboutMSBs || Configuration.Args.Passive) return true;

        List<string> lines = new()
        {
            "[RED]Editing MSBs using WitchyBND is highly discouraged.[/]",
            "For MSB editing, Smithbox is the recommended application. It can be downloaded from Vawser's GitHub.",
            "Editing MSBs with WitchyBND is likely to cause issues.",
            "MSB support is mainly intended for comparisons and version control!",
        };

        var warned = WBUtil.ObnoxiousWarning(lines);
        WarnedAboutMSBs = warned;
        return warned;
    }
}