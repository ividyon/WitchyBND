using SoulsFormats;
using System;
using System.IO;
using System.Xml;

namespace Yabber
{
    static class YBXF3
    {
        public static void Unpack(this BXF3Reader bxf, string bhdName, string bdtName, string targetDir, IProgress<float> progress)
        {
            if (Directory.Exists(targetDir)) YBUtil.Backup(targetDir);

            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;

            using (var xw = XmlWriter.Create($"{targetDir}\\_yabber-bxf3.xml", xws))
            {
                xw.WriteStartElement("bxf3");

                xw.WriteElementString("bhd_filename", bhdName);
                xw.WriteElementString("bdt_filename", bdtName);
                xw.WriteElementString("version", bxf.Version);
                xw.WriteElementString("format", bxf.Format.ToString());
                xw.WriteElementString("bigendian", bxf.BigEndian.ToString());
                xw.WriteElementString("bitbigendian", bxf.BitBigEndian.ToString());

                YBinder.WriteBinderFiles(bxf, xw, targetDir, progress);
                xw.WriteEndElement();
                xw.Close();
            }
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            var bxf = new BXF3();
            var xml = new XmlDocument();
            xml.Load($"{sourceDir}\\_yabber-bxf3.xml");

            string bhdFilename = xml.SelectSingleNode("bxf3/bhd_filename").InnerText;
            string bdtFilename = xml.SelectSingleNode("bxf3/bdt_filename").InnerText;
            bxf.Version = xml.SelectSingleNode("bxf3/version").InnerText;
            bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bxf3/format").InnerText);
            bxf.BigEndian = bool.Parse(xml.SelectSingleNode("bxf3/bigendian").InnerText);
            bxf.BitBigEndian = bool.Parse(xml.SelectSingleNode("bxf3/bitbigendian").InnerText);

            YBinder.ReadBinderFiles(bxf, xml.SelectSingleNode("bxf3/files"), sourceDir);

            string bhdPath = $"{targetDir}\\{bhdFilename}";
            string bdtPath = $"{targetDir}\\{bdtFilename}";

            if (File.Exists(bhdPath)) YBUtil.Backup(bhdPath);
            if (File.Exists(bdtPath)) YBUtil.Backup(bdtPath);

            bxf.Write(bhdPath, bdtPath);
        }
    }
}
