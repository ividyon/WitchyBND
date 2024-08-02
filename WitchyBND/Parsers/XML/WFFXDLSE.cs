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

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var ffx = (file as FFXDLSE)!;
        var xmlPath = GetUnpackDestPath(srcPath, recursive);
        using (var sw = new StreamWriter(xmlPath))
            ffx.XmlSerialize(sw);
        AddLocationToXml(xmlPath, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
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