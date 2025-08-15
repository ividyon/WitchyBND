using System;
using System.IO;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;


public class WFFXDLSE : WSerializedXMLParser
{
    public override string Name => "FFXDLSE";

    public override int Version => WBUtil.WitchyVersionToInt("2.16.0.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<FFXDLSE>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override Type SerializedType => typeof(FFXDLSE);
}