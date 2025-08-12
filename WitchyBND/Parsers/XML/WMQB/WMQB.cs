using SoulsFormats;
using SoulsFormats.Formats.MQB;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMQB : WXMLParser
{

    public override string Name => "MQB";

    public override string XmlTag => "MQB";
    public override int Version => WBUtil.WitchyVersionToInt("2.15.2.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MQB>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }
}