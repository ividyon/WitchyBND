using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR3 : WXMLParser
{
    public override string Name => "FXR3";
    public override int Version => WBUtil.WitchyVersionToInt("2.5.0.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<RSFXR>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        var fxr = (file as RSFXR)!;

        XDocument xDoc = new XDocument();

        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(RSFXR));
            thing.Serialize(xmlWriter, fxr);
        }

        if (Version > 0) xDoc.Root?.Add(new XAttribute(VersionAttributeName, Version.ToString()));

        var destPath = GetUnpackDestPath(srcPath);
        AddLocationToXml(srcPath, xDoc.Root!);
        xDoc.Save(destPath);
    }

    public override void Repack(string srcPath)
    {

        XElement xml = LoadXml(srcPath);

        XmlSerializer serializer = new XmlSerializer(typeof(RSFXR));
        XmlReader xmlReader = xml.CreateReader();

        var fxr = (RSFXR)serializer.Deserialize(xmlReader);
        if (fxr == null)
            throw new Exception();

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        fxr.Write(outPath);
    }
}