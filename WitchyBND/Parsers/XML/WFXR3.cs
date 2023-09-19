using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR3 : WXMLParser
{
    public override string Name => "FXR3";

    public override bool Is(string path)
    {
        return Fxr3.Is(path);
    }

    public override void Unpack(string srcPath)
    {
        var fxr = Fxr3.Read(srcPath);

        XDocument xDoc = new XDocument();

        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(Fxr3));
            thing.Serialize(xmlWriter, fxr);
        }

        var destPath = GetUnpackDestPath(srcPath);
        xDoc.Save(destPath);
        AddLocationToXml(destPath);
    }

    public override void Repack(string srcPath)
    {

        XElement xml = LoadXml(srcPath);

        XmlSerializer serializer = new XmlSerializer(typeof(Fxr3));
        XmlReader xmlReader = xml.CreateReader();

        var fxr = (Fxr3)serializer.Deserialize(xmlReader);
        if (fxr == null)
            throw new Exception();

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        fxr.Write(outPath);
    }
}