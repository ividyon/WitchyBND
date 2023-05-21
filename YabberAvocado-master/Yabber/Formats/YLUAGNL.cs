﻿using SoulsFormats;
using System.Xml;
using System.IO;

namespace Yabber
{
    static class YLUAGNL
    {
        public static void Unpack(this LUAGNL gnl, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;

            using (XmlWriter xw = XmlWriter.Create(targetFile, xws))
            {
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

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            gnl.Write(outPath);
        }
    }
}
