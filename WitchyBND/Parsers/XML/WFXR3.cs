using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR3 : WXMLParser
{
    public override string Name => "FXR3";
    public override int Version => WBUtil.WitchyVersionToInt("2.5.0.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<FXR3>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file, string? recursiveOriginPath)
    {
        var fxr = (file as FXR3)!;

        XDocument xDoc = new XDocument();

        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(FXR3));
            thing.Serialize(xmlWriter, fxr);
        }

        if (Version > 0) xDoc.Root?.Add(new XAttribute(VersionAttributeName, Version.ToString()));

        var destPath = GetUnpackDestPath(srcPath, recursiveOriginPath);
        AddLocationToXml(srcPath, xDoc.Root!);
        xDoc.Save(destPath);
    }

    public override void Repack(string srcPath, string? recursiveOriginPath)
    {

        XElement xml = LoadXml(srcPath);

        XmlSerializer serializer = new XmlSerializer(typeof(FXR3));
        XmlReader xmlReader = xml.CreateReader();

        var fxr = (FXR3)serializer.Deserialize(xmlReader);
        if (fxr == null)
            throw new Exception();

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        fxr.Write(outPath);
    }
}