using System;
using System.IO;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR1 : WSerializedXMLParser
{
    public override string Name => "FXR1";
    
    public override int Version => WBUtil.WitchyVersionToInt("2.16.0.0");

    public override Type SerializedType => typeof(FXR1);

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<FXR1>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path).ToLower();
        return filename.EndsWith(".ffx") ;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        (file as FXR1)!.Flatten();
        base.Unpack(srcPath, file, recursive);
        (file as FXR1)!.Unflatten();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        string outpath = srcPath.Replace(".xml", "");
        FXR1.ReadFromXml(srcPath).Write(outpath);
    }

}