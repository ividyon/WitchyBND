using System.IO;
using System.Xml;
using System.Xml.Linq;
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
        var xmlPath = GetUnpackDestPath(srcPath);
        using (var sw = new StreamWriter(xmlPath))
            ffx.XmlSerialize(sw);
        AddLocationToXml(srcPath);
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