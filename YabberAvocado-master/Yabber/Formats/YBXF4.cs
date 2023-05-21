using SoulsFormats;
using System;
using System.IO;
using System.Xml;

namespace Yabber
{
    static class YBXF4
    {
        public static void Unpack(this BXF4Reader bxf, string bhdName, string bdtName, string targetDir, IProgress<float> progress)
        {
            if (Directory.Exists(targetDir)) YBUtil.Backup(targetDir);

            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;

            using (var xw = XmlWriter.Create($"{targetDir}\\_yabber-bxf4.xml", xws))
            {
                xw.WriteStartElement("bxf4");

                xw.WriteElementString("bhd_filename", bhdName);
                xw.WriteElementString("bdt_filename", bdtName);
                xw.WriteElementString("version", bxf.Version);
                xw.WriteElementString("format", bxf.Format.ToString());
                xw.WriteElementString("bigendian", bxf.BigEndian.ToString());
                xw.WriteElementString("bitbigendian", bxf.BitBigEndian.ToString());
                xw.WriteElementString("unicode", bxf.Unicode.ToString());
                xw.WriteElementString("extended", $"0x{bxf.Extended:X2}");
                xw.WriteElementString("unk04", bxf.Unk04.ToString());
                xw.WriteElementString("unk05", bxf.Unk05.ToString());

                YBinder.WriteBinderFiles(bxf, xw, targetDir, progress);
                xw.WriteEndElement();
                xw.Close();
            }
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            BXF4 bxf = new BXF4();
            XmlDocument xml = new XmlDocument();
            xml.Load($"{sourceDir}\\_yabber-bxf4.xml");

            string bhdFilename = xml.SelectSingleNode("bxf4/bhd_filename").InnerText;
            string bdtFilename = xml.SelectSingleNode("bxf4/bdt_filename").InnerText;
            bxf.Version = xml.SelectSingleNode("bxf4/version").InnerText;
            bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bxf4/format").InnerText);
            bxf.BigEndian = bool.Parse(xml.SelectSingleNode("bxf4/bigendian").InnerText);
            bxf.BitBigEndian = bool.Parse(xml.SelectSingleNode("bxf4/bitbigendian").InnerText);
            bxf.Unicode = bool.Parse(xml.SelectSingleNode("bxf4/unicode").InnerText);
            bxf.Extended = Convert.ToByte(xml.SelectSingleNode("bxf4/extended").InnerText, 16);
            bxf.Unk04 = bool.Parse(xml.SelectSingleNode("bxf4/unk04").InnerText);
            bxf.Unk05 = bool.Parse(xml.SelectSingleNode("bxf4/unk05").InnerText);

            YBinder.ReadBinderFiles(bxf, xml.SelectSingleNode("bxf4/files"), sourceDir);

            string bhdPath = $"{targetDir}\\{bhdFilename}";
            string bdtPath = $"{targetDir}\\{bdtFilename}";

            if (File.Exists(bhdPath)) YBUtil.Backup(bhdPath);
            if (File.Exists(bdtPath)) YBUtil.Backup(bdtPath);

            bxf.Write(bhdPath, bdtPath);
        }
    }
}
