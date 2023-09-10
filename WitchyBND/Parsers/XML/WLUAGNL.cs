using System;
using System.Xml;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WLUAGNL : WXMLParser
{

    public override string Name => "LUAGNL";

    public override bool Is(string path)
    {
        return LUAGNL.Is(path);
    }

    public override void Unpack(string srcPath)
    {
        LUAGNL gnl = LUAGNL.Read(srcPath);
        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create($"{srcPath}.xml", xws);
        xw.WriteStartElement("luagnl");
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

    public override void Repack(string srcPath)
    {
        LUAGNL gnl = new LUAGNL();
        XmlDocument xml = new XmlDocument();
        xml.Load(srcPath);
        gnl.BigEndian = bool.Parse(xml.SelectSingleNode("luagnl/bigendian").InnerText);
        gnl.LongFormat = bool.Parse(xml.SelectSingleNode("luagnl/longformat").InnerText);

        foreach (XmlNode node in xml.SelectNodes("luagnl/globals/global"))
        {
            gnl.Globals.Add(node.InnerText);
        }

        string outPath = GetRepackDestPath(srcPath);
        WBUtil.Backup(outPath);
        gnl.TryWriteSoulsFile(outPath);
    }
}