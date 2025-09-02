using System;
using System.IO;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMTD : WSerializedXMLParser
{
    public override string Name => "MTD";
    
    public override int Version => WBUtil.WitchyVersionToInt("2.15.0.0");

    public override Type SerializedType => typeof(MTD);

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MTD>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path);
        return filename.EndsWith(".mtd") || filename.EndsWith(".mtd.dcx");
    }
}