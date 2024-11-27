using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WLUAGNL : WXMLParser
{

    public override string Name => "LUAGNL";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return Path.GetExtension(path).ToLower() == ".luagnl";
    }

    public override bool? IsSimple(string path)
    {
        return Is(path, null, out _);
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        LUAGNL gnl = LUAGNL.Read(srcPath);
        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath, recursive), xws);
        xw.WriteStartElement("luagnl");

        AddLocationToXml(srcPath, recursive, xw);

        xw.WriteElementString("bigendian", gnl.BigEndian.ToString());
        xw.WriteElementString("longformat", gnl.LongFormat.ToString());
        xw.WriteStartElement("globals");

        foreach (string global in gnl.Globals)
        {
            xw.WriteElementString("global", global);
        }

        xw.WriteEndElement();
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        LUAGNL gnl = new LUAGNL();
        XElement xml = LoadXml(srcPath);
        gnl.BigEndian = bool.Parse(xml.Element("bigendian").Value);
        gnl.LongFormat = bool.Parse(xml.Element("longformat").Value);

        foreach (XElement node in xml.Element("globals")?.Elements("global") ?? new XElement[]{})
        {
            gnl.Globals.Add(node.Value);
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        gnl.TryWriteSoulsFile(outPath);
    }
}