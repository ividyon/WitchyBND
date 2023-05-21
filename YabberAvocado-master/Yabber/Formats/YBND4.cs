using SoulsFormats;
using System;
using System.IO;
using System.Xml;

namespace Yabber
{
    static class YBND4
    {
        public static void Unpack(this BND4Reader bnd, string sourceName, string targetDir, IProgress<float> progress)
        {
            if (Directory.Exists(targetDir)) YBUtil.Backup(targetDir);

            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;

            using (var xw = XmlWriter.Create($"{targetDir}\\_yabber-bnd4.xml", xws))
            {
                xw.WriteStartElement("bnd4");

                xw.WriteElementString("filename", sourceName);
                xw.WriteElementString("compression", bnd.Compression.ToString());
                xw.WriteElementString("version", bnd.Version);
                xw.WriteElementString("format", bnd.Format.ToString());
                xw.WriteElementString("bigendian", bnd.BigEndian.ToString());
                xw.WriteElementString("bitbigendian", bnd.BitBigEndian.ToString());
                xw.WriteElementString("unicode", bnd.Unicode.ToString());
                xw.WriteElementString("extended", $"0x{bnd.Extended:X2}");
                xw.WriteElementString("unk04", bnd.Unk04.ToString());
                xw.WriteElementString("unk05", bnd.Unk05.ToString());
                YBinder.WriteBinderFiles(bnd, xw, targetDir, progress);
                xw.WriteEndElement();
                xw.Close();
            }
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            var bnd = new BND4();
            var xml = new XmlDocument();
            xml.Load($"{sourceDir}\\_yabber-bnd4.xml");

            string filename = xml.SelectSingleNode("bnd4/filename").InnerText;
            Enum.TryParse(xml.SelectSingleNode("bnd4/compression")?.InnerText ?? "None", out DCX.Type cmpr);
            bnd.Compression = cmpr;
            bnd.Version = xml.SelectSingleNode("bnd4/version").InnerText;
            bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bnd4/format").InnerText);
            bnd.BigEndian = bool.Parse(xml.SelectSingleNode("bnd4/bigendian").InnerText);
            bnd.BitBigEndian = bool.Parse(xml.SelectSingleNode("bnd4/bitbigendian").InnerText);
            bnd.Unicode = bool.Parse(xml.SelectSingleNode("bnd4/unicode").InnerText);
            bnd.Extended = Convert.ToByte(xml.SelectSingleNode("bnd4/extended").InnerText, 16);
            bnd.Unk04 = bool.Parse(xml.SelectSingleNode("bnd4/unk04").InnerText);
            bnd.Unk05 = bool.Parse(xml.SelectSingleNode("bnd4/unk05").InnerText);
            YBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd4/files"), sourceDir);

            string outPath = $"{targetDir}\\{filename}";

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            bnd.Write(outPath);
        }
    }
}
