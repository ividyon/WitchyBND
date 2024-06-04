using System.Collections.Generic;
using WitchyLib;

namespace WitchyBND.Parsers;
public partial class WMSBEFolder : WFolderParser
{
    public override string Name => "MSBE (Folder)";
    public override string XmlTag => "msbe";
    public override bool HasPreprocess => true;
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;
    public override int Version => WBUtil.WitchyVersionToInt("2.8.0.1");
    public override bool Preprocess(string srcPath)
    {
        return false; // Preprocess them all to perform WarnAboutMSBs
    }
    private static bool WarnedAboutMSBs { get; set; }

    public static bool WarnAboutMSBs()
    {
        if (Configuration.Active.Expert || WarnedAboutMSBs || Configuration.Active.Passive) return true;

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