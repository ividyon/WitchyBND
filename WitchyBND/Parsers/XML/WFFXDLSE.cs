using System.IO;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFFXDLSE : WXMLParser
{
    public override string Name => "FFXDLSE";
    public override bool Is(string path)
    {
        return FFXDLSE.Is(path);
    }

    public override void Unpack(string srcPath)
    {
        FFXDLSE ffx = FFXDLSE.Read(srcPath);
        using (var sw = new StreamWriter($"{srcPath}.xml"))
            ffx.XmlSerialize(sw);
    }

    public override void Repack(string srcPath)
    {
        FFXDLSE ffx;
        using (var sr = new StreamReader(srcPath))
            ffx = FFXDLSE.XmlDeserialize(sr);

        string outPath = srcPath.Replace(".ffx.xml", ".ffx").Replace(".ffx.dcx.xml", ".ffx.dcx");
        WBUtil.Backup(outPath);
        ffx.TryWriteSoulsFile(outPath);
    }
}