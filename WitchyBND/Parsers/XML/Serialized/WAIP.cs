using System;
using System.IO;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WAIP : WSerializedXMLParser
{
    public override string Name => "AIP";

    public override Type SerializedType => typeof(AIP);

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<AIP>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = OSPath.GetFileName(path).ToLower();
        return filename.EndsWith(".aip");
    }
}