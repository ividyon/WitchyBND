using SoulsFormats;
using System;
using System.IO;
using System.Xml;
using WitchyLib;

namespace WitchyBND
{
    static class WBXF3
    {
        public static void Unpack(this BXF3Reader bxf, string bhdName, string bdtName, string targetDir, IProgress<float> progress)
        {
            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            var xw = XmlWriter.Create($"{targetDir}\\_witchy-bxf3.xml", xws);
            xw.WriteStartElement("bxf3");

            xw.WriteElementString("bhd_filename", bhdName);
            xw.WriteElementString("bdt_filename", bdtName);
            xw.WriteElementString("version", bxf.Version);
            xw.WriteElementString("format", bxf.Format.ToString());
            xw.WriteElementString("bigendian", bxf.BigEndian.ToString());
            xw.WriteElementString("bitbigendian", bxf.BitBigEndian.ToString());

            WBinder.WriteBinderFiles(bxf, xw, targetDir, progress);
            xw.WriteEndElement();
            xw.Close();
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            var bxf = new BXF3();
            var xml = new XmlDocument();

            xml.Load(WBUtil.GetXmlPath("bxf3", sourceDir));

            string bhdFilename = xml.SelectSingleNode("bxf3/bhd_filename").InnerText;
            string bdtFilename = xml.SelectSingleNode("bxf3/bdt_filename").InnerText;
            string root = xml.SelectSingleNode("bxf3/root")?.InnerText ?? "";

            bxf.Version = xml.SelectSingleNode("bxf3/version").InnerText;
            bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bxf3/format").InnerText);
            bxf.BigEndian = bool.Parse(xml.SelectSingleNode("bxf3/bigendian").InnerText);
            bxf.BitBigEndian = bool.Parse(xml.SelectSingleNode("bxf3/bitbigendian").InnerText);

            WBinder.ReadBinderFiles(bxf, xml.SelectSingleNode("bxf3/files"), sourceDir, root);

            string bhdPath = $"{targetDir}\\{bhdFilename}";
            WBUtil.Backup(bhdPath);
            string bdtPath = $"{targetDir}\\{bdtFilename}";
            WBUtil.Backup(bdtPath);
            bxf.Write(bhdPath, bdtPath);
        }
    }
}
