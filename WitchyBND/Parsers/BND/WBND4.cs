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

        string xmlPath = Path.Combine(path, "_witchy-bnd4.xml");
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == "bnd4";
    }

    public override void Unpack(string srcPath)
        {
            var bnd = new BND4Reader(srcPath);
            string srcName = Path.GetFileName(srcPath);
            string destDir = GetUnpackDestPath(srcPath);
            Directory.CreateDirectory(destDir);

            var root = "";
            if (Binder.HasNames(bnd.Format))
            {
                root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
            }

            XElement files = WriteBinderFiles(bnd, destDir, root);

            var xml =
                new XElement("bnd4",
                    new XElement("filename", srcName),
                    new XElement("compression", bnd.Compression.ToString()),
                    new XElement("version", bnd.Version),
                    new XElement("format", bnd.Format.ToString()),
                    new XElement("bigendian", bnd.BigEndian.ToString()),
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

            var doc = XDocument.Load(WBUtil.GetXmlPath("bnd4", srcPath));
            if (doc.Root == null) throw new XmlException("XML has no root");
            var myXml = doc.Root;
            string filename = doc.Root.Elements("filename").First().Value;

            string root = myXml.Elements("root").Any() ? myXml.Elements("root").First().Value : "";

            string compString = myXml.Elements("compression").Any() ? myXml.Elements("compression").First().Value : "None";
            Enum.TryParse(compString, out DCX.Type compression);
            bnd.Compression = compression;

            bnd.Version = myXml.Elements("version").First().Value;
            bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), myXml.Elements("format").First().Value);
            bnd.BigEndian = bool.Parse(myXml.Elements("bigendian").First().Value);
            bnd.BitBigEndian = bool.Parse(myXml.Elements("bitbigendian").First().Value);
            bnd.Unicode = bool.Parse(myXml.Elements("unicode").First().Value);
            bnd.Extended = Convert.ToByte(myXml.Elements("extended").First().Value, 16);
            bnd.Unk04 = bool.Parse(myXml.Elements("unk04").First().Value);
            bnd.Unk05 = bool.Parse(myXml.Elements("unk05").First().Value);

            WBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd4/files"), srcPath, root);


            var destPath = GetRepackDestPath(srcPath, filename);

            string outPath = $"{destPath}\\{filename}";
            WBUtil.Backup(outPath);
            bnd.Write(outPath);
        }

        public override string GetRepackDestPath(string srcDirPath, string destFileName)
        {
            throw new NotImplementedException();
        }
}