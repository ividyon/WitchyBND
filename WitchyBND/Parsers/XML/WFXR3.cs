using System;
using System.IO;
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
    public override int Version => WBUtil.WitchyVersionToInt("2.2.1.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<RSFXR>(path, data, out file);
    }

    public override int GetUnpackedVersion(string path)
    {
        var doc = XDocument.Load(path);
        var attr = doc.Root?.Attribute(VersionAttributeName);
        if (attr == null) return 0;
        return int.Parse(attr.Value);
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

        xDoc.Root?.Add(new XAttribute(VersionAttributeName, Version.ToString()));

        var destPath = GetUnpackDestPath(srcPath);
        xDoc.Save(destPath);
        AddLocationToXml(destPath);
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