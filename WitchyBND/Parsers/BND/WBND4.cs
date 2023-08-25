using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBND4 : WBinderParser
{
    public override string Name => "BND4";

    public override bool Is(string path)
    {
        return BND4.Is(path);
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        var xmlPaths = Directory.GetFiles(path, "_witchy-bnd4.xml");
        if (xmlPaths.Length == 0) return false;

        XmlDocument xDoc = new XmlDocument();

        throw new NotImplementedException();
    }

    public override void Unpack(string srcPath)
        {
            var bnd = new BND4Reader(srcPath);
            var srcName = Path.GetFileName(srcPath);
            var destDir = GetUnpackDestPath(srcPath);
            Directory.CreateDirectory(destDir);

            string root = "";
            if (Binder.HasNames(bnd.Format))
            {
                root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
            }

            XElement files = new XElement("files", WriteBinderFiles(bnd, destDir, root));

            XElement xml =
                new XElement("bnd4",
                    new XElement("filename", srcName),
                    new XElement("compression", bnd.Compression.ToString()),
                    new XElement("version", bnd.Version),
                    new XElement("format", bnd.Format.ToString()),
                    new XElement("format", bnd.Format.ToString()),
                    new XElement("bigendian", bnd.BigEndian, ToString()),
                    new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                    new XElement("unicode", bnd.Unicode.ToString()),
                    new XElement("extended", $"0x{bnd.Extended:X2}"),
                    new XElement("unk04", bnd.Unk04.ToString()),
                    new XElement("unk05", bnd.Unk05.ToString()),
                    files);

            if (!string.IsNullOrEmpty(root))
                files.AddBeforeSelf(new XElement("root", root));

            var xw = XmlWriter.Create($"{destDir}\\_witchy-bnd4.xml", new XmlWriterSettings
            {
                Indent = true
            });
            xml.WriteTo(xw);
            xw.Close();
        }

        public override void Repack(string srcPath)
        {
            var bnd = new BND4();
            var xml = new XmlDocument();

            xml.Load(WBUtil.GetXmlPath("bnd4", srcPath));

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

            WBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd4/files"), srcPath, root);


            var destPath = GetRepackDestPath(srcPath, filename);

            string outPath = $"{destPath}\\{filename}";
            WBUtil.Backup(outPath);
            bnd.Write(outPath);
        }

        public override string GetUnpackDestPath(string srcPath)
        {
            throw new NotImplementedException();
        }

        public override string GetRepackDestPath(string srcDirPath, string destFileName)
        {
            throw new NotImplementedException();
        }
}