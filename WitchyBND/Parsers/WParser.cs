using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public enum WFileParserVerb
{
    Unpack = 0,
    Serialize = 1,
}
public abstract class WFileParser
{
    public virtual WFileParserVerb Verb => WFileParserVerb.Unpack;
    public abstract string Name { get; }
    public abstract bool Is(string path);
    public abstract bool IsUnpacked(string path);
    public abstract void Unpack(string srcPath);
    public abstract void Repack(string srcPath);
}

public abstract class WSingleFileParser : WFileParser
{
    public abstract string GetUnpackDestPath(string srcPath);
    public abstract string GetRepackDestPath(string srcPath);
}

public abstract class WFolderParser : WFileParser
{
    public override WFileParserVerb Verb => WFileParserVerb.Unpack;

    public virtual string GetUnpackDestPath(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        string fileName = Path.GetFileName(srcPath);
        return $"{sourceDir}\\{fileName.Replace('.', '-')}";
    }

    public abstract string GetRepackDestPath(string srcDirPath, string destFileName);
}

public abstract class WBinderParser : WFolderParser
{
    public static XElement WriteBinderFiles(BinderReader bnd, string destDirPath, string root)
    {
        XElement files = new XElement("files");
        var pathCounts = new Dictionary<string, int>();

        for (int i = 0; i < bnd.Files.Count; i++)
        {
            BinderFileHeader file = bnd.Files[i];

            string path;
            if (Binder.HasNames(bnd.Format))
            {
                path = WBUtil.UnrootBNDPath(file.Name, root);
            }
            else if (Binder.HasIDs(bnd.Format))
            {
                path = file.ID.ToString();
            }
            else
            {
                path = i.ToString();
            }

            var fileElement = new XElement("file",
            new XElement("flags", file.Flags.ToString()));

            if (Binder.HasIDs(bnd.Format))
                fileElement.Add(new XElement("id", file.ID.ToString()));

            fileElement.Add(new XElement("path", path));

            string suffix = "";
            if (pathCounts.ContainsKey(path))
            {
                pathCounts[path]++;
                suffix = $" ({pathCounts[path]})";
                fileElement.Add(new XElement("suffix", suffix));
            }
            else
            {
                pathCounts[path] = 1;
            }

            if (file.CompressionType != DCX.Type.Zlib)
                fileElement.Add("compression_type", file.CompressionType.ToString());

            byte[] bytes = bnd.ReadFile(file);
            string outPath =
                $@"{destDirPath}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, bytes);

            files.Add(fileElement);
        }

        return files;
    }

    public static void ReadBinderFiles(IBinder bnd, XmlNode filesNode, string destPath, string root)
    {
        foreach (XmlNode fileNode in filesNode.SelectNodes("file"))
        {
            if (fileNode.SelectSingleNode("path") == null)
                throw new FriendlyException("File node missing path tag.");

            string strFlags = fileNode.SelectSingleNode("flags")?.InnerText ?? "Flag1";
            string strID = fileNode.SelectSingleNode("id")?.InnerText ?? "-1";
            string path = fileNode.SelectSingleNode("path").InnerText;
            string suffix = fileNode.SelectSingleNode("suffix")?.InnerText ?? "";
            string strCompression =
                fileNode.SelectSingleNode("compression_type")?.InnerText ?? DCX.Type.Zlib.ToString();
            string name = root + path;

            if (!Enum.TryParse(strFlags, out Binder.FileFlags flags))
                throw new FriendlyException(
                    $"Could not parse file flags: {strFlags}\nFlags must be comma-separated list of flags.");

            if (!int.TryParse(strID, out int id))
                throw new FriendlyException($"Could not parse file ID: {strID}\nID must be a 32-bit signed integer.");

            if (!Enum.TryParse(strCompression, out DCX.Type compressionType))
                throw new FriendlyException(
                    $"Could not parse compression type: {strCompression}\nCompression type must be a valid DCX Type.");

            string inPath =
                $@"{destPath}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");

            byte[] bytes = File.ReadAllBytes(inPath);
            bnd.Files.Add(new BinderFile(flags, id, name, bytes)
            {
                CompressionType = compressionType
            });
        }
    }
}

public abstract class WXMLParser : WSingleFileParser
{
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;
    public override string GetUnpackDestPath(string srcPath)
    {
        return $"{srcPath}.xml";
    }

    public override string GetRepackDestPath(string srcPath)
    {
        return srcPath.Replace(".xml", "");
    }
}