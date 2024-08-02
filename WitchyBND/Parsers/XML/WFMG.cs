using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFMG : WXMLParser
{

    public override string Name => "FMG";

    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        return Path.GetExtension(path).ToLower() == ".fmg";
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        FMG fmg = FMG.Read(srcPath);
        XmlWriterSettings xws = new XmlWriterSettings();
        // You need Indent for it to write newlines
        xws.Indent = true;
        // But don't actually indent so there's more room for the text
        xws.IndentChars = "";
        XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath, recursive), xws);
        xw.WriteStartElement("fmg");

        xw.WriteElementString("filename", Path.GetFileName(srcPath));
        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            xw.WriteElementString("sourcePath", Path.GetDirectoryName(srcPath));

        xw.WriteElementString("compression", fmg.Compression.ToString());
        xw.WriteElementString("version", fmg.Version.ToString());
        xw.WriteElementString("bigendian", fmg.BigEndian.ToString());
        xw.WriteStartElement("entries");

        fmg.Entries.Sort((e1, e2) => e1.ID.CompareTo(e2.ID));
        foreach (FMG.Entry entry in fmg.Entries)
        {
            xw.WriteStartElement("text");
            xw.WriteAttributeString("id", entry.ID.ToString());
            xw.WriteString(entry.Text ?? "%null%");
            xw.WriteEndElement();
        }

        xw.WriteEndElement();
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        FMG fmg = new FMG();

        XElement xml = LoadXml(srcPath);
        Enum.TryParse(xml.Element("compression")?.Value ?? "None", out DCX.Type compression);
        fmg.Compression = compression;

        fmg.Version = (FMG.FMGVersion)Enum.Parse(typeof(FMG.FMGVersion), xml.Element("version").Value);
        fmg.BigEndian = bool.Parse(xml.Element("bigendian").Value);

        foreach (XElement textNode in xml.Element("entries").Elements("text"))
        {
            int id = int.Parse(textNode.Attribute("id").Value);
            // \r\n is drawn as two newlines ingame
            string text = textNode.Value.Replace("\r", "");
            if (text == "%null%")
                text = null;
            fmg.Entries.Add(new FMG.Entry(id, text));
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        fmg.TryWriteSoulsFile(outPath);
    }
}