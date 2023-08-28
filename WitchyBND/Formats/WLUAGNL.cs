using SoulsFormats;
using System.Xml;
using WitchyLib;

namespace WitchyBND
{
    static class WLUAGNL
    {
        public static void Unpack(this LUAGNL gnl, string sourceFile)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            XmlWriter xw = XmlWriter.Create($"{sourceFile}.xml", xws);
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

        public static void Repack(string sourceFile)
        {
            LUAGNL gnl = new LUAGNL();
            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);
            gnl.BigEndian = bool.Parse(xml.SelectSingleNode("luagnl/bigendian").InnerText);
            gnl.LongFormat = bool.Parse(xml.SelectSingleNode("luagnl/longformat").InnerText);

            foreach (XmlNode node in xml.SelectNodes("luagnl/globals/global"))
            {
                gnl.Globals.Add(node.InnerText);
            }

            string outPath = sourceFile.Replace(".luagnl.xml", ".luagnl");
            WBUtil.Backup(outPath);
            gnl.TryWriteSoulsFile(outPath);
        }
    }
}
