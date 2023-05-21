using SoulsFormats;
using System;
using System.Xml;
using System.IO;

namespace Yabber
{
    static class YFMG
    {
        public static void Unpack(this FMG fmg, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            XmlWriterSettings xws = new XmlWriterSettings();
            // You need Indent for it to write newlines
            xws.Indent = true;
            // But don't actually indent so there's more room for the text
            xws.IndentChars = "";

            using (XmlWriter xw = XmlWriter.Create(targetFile, xws))
            {
                xw.WriteStartElement("fmg");
                xw.WriteElementString("compression", fmg.Compression.ToString());
                xw.WriteElementString("version", fmg.Version.ToString());
                xw.WriteElementString("bigendian", fmg.BigEndian.ToString());
                xw.WriteStartElement("entries");

                // I think they're sorted already, but whatever
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
        }

        public static void Repack(string sourceFile)
        {
            FMG fmg = new FMG();
            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);
            Enum.TryParse(xml.SelectSingleNode("fmg/compression")?.InnerText ?? "None", out DCX.Type cmpr);
            fmg.Compression = cmpr;
            fmg.Version = (FMG.FMGVersion)Enum.Parse(typeof(FMG.FMGVersion), xml.SelectSingleNode("fmg/version").InnerText);
            fmg.BigEndian = bool.Parse(xml.SelectSingleNode("fmg/bigendian").InnerText);

            foreach (XmlNode textNode in xml.SelectNodes("fmg/entries/text"))
            {
                int id = int.Parse(textNode.Attributes["id"].InnerText);
                // \r\n is drawn as two newlines ingame
                string text = textNode.InnerText.Replace("\r", "");
                if (text == "%null%")
                    text = null;
                fmg.Entries.Add(new FMG.Entry(id, text));
            }

            string outPath = sourceFile.Replace(".fmg.xml", ".fmg");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            fmg.Write(outPath);
        }
    }
}
