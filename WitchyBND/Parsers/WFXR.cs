using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFXR : WXMLParser
{
    public override string Name => "FXR";

    public override bool Is(string path)
    {
        return Fxr3.Is(path);
    }

    public override bool IsUnpacked(string path)
    {
        XmlDocument xDoc = new XmlDocument();

        try
        {
            xDoc.Load(path);
        }
        catch (XmlException)
        {
            return false;
        }

        XmlElement root = xDoc.DocumentElement;
        return root != null && root.Name == nameof(Fxr3);
    }

    public override void Unpack(string path)
    {
        var fxr = Fxr3.Read(path);

        XDocument xDoc = new XDocument();

        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(Fxr3));
            thing.Serialize(xmlWriter, fxr);
        }

        xDoc.Save(GetUnpackDestPath(path));
    }

    public override void Repack(string path)
    {
        XDocument XML = XDocument.Load(path);
        XmlSerializer serializer = new XmlSerializer(typeof(Fxr3));
        XmlReader xmlReader = XML.CreateReader();

        var fxr = (Fxr3)serializer.Deserialize(xmlReader);
        if (fxr == null)
            throw new Exception();

        string outPath = GetRepackDestPath(path);
        WBUtil.Backup(outPath);
        fxr.Write(outPath);
    }
}