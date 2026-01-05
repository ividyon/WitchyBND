using System;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WENFL : WSerializedXMLParser
{

    public override string Name => "ENFL";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<ENFL>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = OSPath.GetFileName(path).ToLower();
        return filename.EndsWith(".entryfilelist");
    }

    public override Type SerializedType => typeof(ENFL);
}