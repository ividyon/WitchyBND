using System.IO;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFFXDLSE : WXMLParser
{
    public override string Name => "FFXDLSE";
    public override bool Is(string path, byte[]? data, out ISoulsFile file)
    {
        return IsRead<FFXDLSE>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        var ffx = (file as FFXDLSE)!;
        var xmlPath = GetUnpackDestPath(srcPath);
        using (var sw = new StreamWriter(xmlPath))
            ffx.XmlSerialize(sw);
        AddLocationToXml(xmlPath, srcPath);
    }

    public override void Repack(string srcPath)
    {
        FFXDLSE ffx;
        using (var sr = new StreamReader(srcPath))
            ffx = FFXDLSE.XmlDeserialize(sr);

        XElement xml = LoadXml(srcPath);

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        ffx.TryWriteSoulsFile(outPath);
    }
}