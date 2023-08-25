using SoulsFormats;
using System;
using System.IO;
using System.Xml;
using WitchyLib;

namespace WitchyBND
{
    static class WBND3
    {
        public static void Unpack(this BND3Reader bnd, string sourceName, string targetDir, IProgress<float> progress)
        {
            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            var xw = XmlWriter.Create($"{targetDir}\\_witchy-bnd3.xml", xws);
            xw.WriteStartElement("bnd3");

            xw.WriteElementString("filename", sourceName);
            WBUtil.XmlWriteCompression(xw, bnd.Compression, bnd.CompressionLevel);
            xw.WriteElementString("version", bnd.Version);
            xw.WriteElementString("format", bnd.Format.ToString());
            xw.WriteElementString("bigendian", bnd.BigEndian.ToString());
            xw.WriteElementString("bitbigendian", bnd.BitBigEndian.ToString());
            xw.WriteElementString("unk18", $"0x{bnd.Unk18:X}");
            WBinder.WriteBinderFiles(bnd, xw, targetDir, progress);

            xw.WriteEndElement();
            xw.Close();
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            var bnd = new BND3();
            var xml = new XmlDocument();

            xml.Load(WBUtil.GetXmlPath("bnd3", sourceDir));

            if (xml.SelectSingleNode("bnd3/filename") == null)
                throw new FriendlyException("Missing filename tag.");

            string filename = xml.SelectSingleNode("bnd3/filename").InnerText;
            string root = xml.SelectSingleNode("bnd3/root")?.InnerText ?? "";

            bnd.Version = xml.SelectSingleNode("bnd3/version")?.InnerText ?? "07D7R6";
            string strFormat = xml.SelectSingleNode("bnd3/format")?.InnerText ?? "IDs, Names1, Names2, Compression";
            string strBigEndian = xml.SelectSingleNode("bnd3/bigendian")?.InnerText ?? "False";
            string strBitBigEndian = xml.SelectSingleNode("bnd3/bitbigendian")?.InnerText ?? "False";
            string strUnk18 = xml.SelectSingleNode("bnd3/unk18")?.InnerText ?? "0x0";

            WBUtil.XmlReadCompression(xml, "bnd3", out DCX.Type compression, out int compressionLevel);
            bnd.Compression = compression;
            bnd.CompressionLevel = compressionLevel;

            try
            {
                bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), strFormat);
            }
            catch
            {
                throw new FriendlyException($"Could not parse format: {strFormat}\nFormat must be a comma-separated list of flags.");
            }

            if (!bool.TryParse(strBigEndian, out bool bigEndian))
                throw new FriendlyException($"Could not parse big-endianness: {strBigEndian}\nBig-endianness must be true or false.");
            bnd.BigEndian = bigEndian;

            if (!bool.TryParse(strBitBigEndian, out bool bitBigEndian))
                throw new FriendlyException($"Could not parse bit big-endianness: {strBitBigEndian}\nBit big-endianness must be true or false.");
            bnd.BitBigEndian = bitBigEndian;

            try
            {
                bnd.Unk18 = Convert.ToInt32(strUnk18, 16);
            }
            catch
            {
                throw new FriendlyException($"Could not parse unk18: {strUnk18}\nUnk18 must be a hex value.");
            }

            if (xml.SelectSingleNode("bnd3/files") != null)
                WBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd3/files"), sourceDir, root);

            string outPath = $"{targetDir}\\{filename}";
            WBUtil.Backup(outPath);
            bnd.Write(outPath);
        }
    }
}
