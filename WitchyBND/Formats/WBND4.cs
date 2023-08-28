using SoulsFormats;
using System;
using System.IO;
using System.Xml;
using WitchyLib;

namespace WitchyBND
{
    static class WBND4
    {
        public static void Unpack(this BND4Reader bnd, string sourceName, string targetDir, IProgress<float> progress, WBUtil.GameType? game = null)
        {
            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            var xw = XmlWriter.Create($"{targetDir}\\_witchy-bnd4.xml", xws);
            xw.WriteStartElement("bnd4");

            xw.WriteElementString("filename", sourceName);
            if (game != null)
                xw.WriteElementString("game", game.ToString());
            xw.WriteElementString("compression", bnd.Compression.ToString());
            xw.WriteElementString("version", bnd.Version);
            xw.WriteElementString("format", bnd.Format.ToString());
            xw.WriteElementString("bigendian", bnd.BigEndian.ToString());
            xw.WriteElementString("bitbigendian", bnd.BitBigEndian.ToString());
            xw.WriteElementString("unicode", bnd.Unicode.ToString());
            xw.WriteElementString("extended", $"0x{bnd.Extended:X2}");
            xw.WriteElementString("unk04", bnd.Unk04.ToString());
            xw.WriteElementString("unk05", bnd.Unk05.ToString());
            WBinder.WriteBinderFiles(bnd, xw, targetDir, progress);
            xw.WriteEndElement();
            xw.Close();
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            var bnd = new BND4();
            var xml = new XmlDocument();

            xml.Load(WBUtil.GetXmlPath("bnd4", sourceDir));

            string filename = xml.SelectSingleNode("bnd4/filename").InnerText;
            var root = xml.SelectSingleNode("bnd4/root")?.InnerText ?? "";

            Enum.TryParse(xml.SelectSingleNode("bnd4/compression")?.InnerText ?? "None", out DCX.Type compression);
            bnd.Compression = compression;

            bnd.Version = xml.SelectSingleNode("bnd4/version").InnerText;
            bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bnd4/format").InnerText);
            bnd.BigEndian = bool.Parse(xml.SelectSingleNode("bnd4/bigendian").InnerText);
            bnd.BitBigEndian = bool.Parse(xml.SelectSingleNode("bnd4/bitbigendian").InnerText);
            bnd.Unicode = bool.Parse(xml.SelectSingleNode("bnd4/unicode").InnerText);
            bnd.Extended = Convert.ToByte(xml.SelectSingleNode("bnd4/extended").InnerText, 16);
            bnd.Unk04 = bool.Parse(xml.SelectSingleNode("bnd4/unk04").InnerText);
            bnd.Unk05 = bool.Parse(xml.SelectSingleNode("bnd4/unk05").InnerText);

            WBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd4/files"), sourceDir, root);

            string outPath = $"{targetDir}\\{filename}";
            WBUtil.Backup(outPath);
            bnd.TryWriteSoulsFile(outPath);
        }
    }
}
