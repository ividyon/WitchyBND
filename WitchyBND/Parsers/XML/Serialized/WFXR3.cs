using System;
using System.IO;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR3 : WSerializedXMLParser
{
    public override string Name => "FXR3";
    
    public override int Version => WBUtil.WitchyVersionToInt("2.15.0.0");

    public override Type SerializedType => typeof(FXR3);

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<FXR3>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = OSPath.GetFileName(path).ToLower();
        return filename.EndsWith(".fxr") ;
    }
}