using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WANIBND4 : WBinderParser
{
    public override string Name => "ANIBND4";
    public override string XmlTag => "anibnd4";

    public string[] ProcessedExtensions =
    [
        ".hkx",
        ".tae"
    ];

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        path = path.ToLower();
        return Configuration.Active.Bnd &&
               path.Contains(".anibnd") && IsRead<BND4>(path, data, out file);
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        return $"{base.GetUnpackDestPath(srcPath)}-wanibnd";
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        BND4 bnd = (file as BND4)!;
        var destDir = GetUnpackDestPath(srcPath);
        var srcName = Path.GetFileName(srcPath);
        Directory.CreateDirectory(destDir);

        var filename = new XElement("filename", srcName);
        if (!bnd.Files.Any())
            throw new FriendlyException("ANIBND is empty, no need to unpack.");

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        var xml = new XElement(XmlTag,
            filename,
            new XElement("compression", bnd.Compression.ToString()),
            new XElement("version", bnd.Version),
            new XElement("format", bnd.Format.ToString()),
            new XElement("bigendian", bnd.BigEndian.ToString()),
            new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
            new XElement("unicode", bnd.Unicode.ToString()),
            new XElement("extended", $"0x{bnd.Extended:X2}"),
            new XElement("unk04", bnd.Unk04.ToString()),
            new XElement("unk05", bnd.Unk05.ToString())
        );

        if (Version > 0) xml.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            filename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        if (!string.IsNullOrEmpty(root))
            xml.Add(new XElement("root", root));

        var newFiles = new ConcurrentBag<BinderFile>();
        var resultingPaths = new ConcurrentStack<string>();

        void Callback(BinderFile bndFile)
        {
            if (!ProcessedExtensions.Contains(Path.GetExtension(bndFile.Name)) ||
                bndFile.ID >= 7000000 && bndFile.ID <= 7999999 || // BB behaviors
                bndFile.Name.ToLower().EndsWith("skeleton.hkx") ||
                (bndFile.Name.EndsWith(".tae") && Path.GetFileName(bndFile.Name).StartsWith("c"))
               )
            {
                newFiles.Add(bndFile);
                return;
            }

            byte[] bytes = bndFile.Bytes;
            var path = WBUtil.UnrootBNDPath(bndFile.Name, root);
            var destPath = Path.Combine(destDir, path);
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.WriteAllBytes(destPath, bytes);
            resultingPaths.Push(destPath);
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(bnd.Files, Callback);
        else
            bnd.Files.ForEach(Callback);

        // Remaining files
        bnd.Files = newFiles.OrderBy(a => a.ID).ToList();

        XElement files = WriteBinderFiles(bnd, destDir, root);
        xml.Add(files);

        using var xw = XmlWriter.Create($"{destDir}\\{GetFolderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true,
        });
        xml.WriteTo(xw);
        xw.Close();

        if (Configuration.Active.Recursive)
        {
            ParseMode.ParseFiles(resultingPaths.ToList(), true);
        }
    }

    public override void Repack(string srcPath)
    {
        var bnd = new BND4();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        Enum.TryParse(xml.Element("compression")?.Value ?? "None", out DCX.Type compression);
        bnd.Compression = compression;

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bnd.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bnd.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bnd.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        var filesElement = xml.Element("files");
        if (filesElement != null)
            ReadBinderFiles(bnd, filesElement, srcPath, root);
        var pathsToSkip = filesElement != null
            ? filesElement.Elements("file").Select(file => Path.Combine(root, file.Element("path")!.Value)).ToList()
            : new List<string>();

        ConcurrentBag<BinderFile> fileBag = new();

        void FileCallback(string ext, string filePath)
        {
            var pathDir = filePath.Substring(srcPath.Length + 1);
            var binderPath = Path.Combine(root, pathDir);
            if (pathsToSkip.Contains(binderPath)) return;
            RecursiveRepackFile(filePath);

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            bool isPlayer = pathDir.Contains("c0000");
            if (ext == ".hkx")
            {
                int baseHkxId = 1000000000;
                if (fileName.ToLower() == "skeleton")
                {
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, isPlayer ? 1000000 : 4000000, binderPath,
                        File.ReadAllBytes(filePath)));
                }
                else
                {
                    int fileId;

                    try
                    {
                        var split = fileName.Substring(1).Split("_").Select(a => int.Parse(a)).ToArray();
                        int taeId = split[0];
                        int animId = split[1];
                        fileId = baseHkxId + (1000000 * taeId) + animId;
                    }
                    catch (Exception)
                    {
                        errorService.RegisterNotice($"Skipping HKX with unrecognized file name pattern: {pathDir}");
                        return;
                    }

                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, fileId, binderPath,
                        File.ReadAllBytes(filePath)));
                }
            }
            else if (ext == ".tae")
            {
                if (isPlayer)
                {
                    int taeId;
                    try
                    {
                        taeId = int.Parse(fileName.Substring(1));
                    }
                    catch (Exception)
                    {
                        errorService.RegisterNotice($"Skipping TAE with unrecognized file name pattern: {pathDir}");
                        return;
                    }

                    taeId = 5000000 + taeId;
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, taeId, binderPath,
                        File.ReadAllBytes(filePath)));
                }
                else
                {
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, 3000000, binderPath,
                        File.ReadAllBytes(filePath)));
                }
            }
            else
            {
                throw new InvalidDataException($"Unrecognized extension {ext}");
            }
        }

        void ExtensionCallback(string ext)
        {
            if (Configuration.Active.Parallel)
                Parallel.ForEach(Directory.EnumerateFiles(srcPath, $"*{ext}", SearchOption.AllDirectories),
                    filePath => FileCallback(ext, filePath));
            else
            {
                foreach (string filePath in Directory.EnumerateFiles(srcPath, $"*{ext}", SearchOption.AllDirectories))
                {
                    FileCallback(ext, filePath);
                }
            }
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(ProcessedExtensions, ExtensionCallback);
        else
        {
            foreach (string ext in ProcessedExtensions)
            {
                ExtensionCallback(ext);
            }
        }

        bnd.Files = bnd.Files.Union(fileBag.ToList()).OrderBy(a => a.ID).ToList();

        var destPath = GetRepackDestPath(srcPath, xml);

        WBUtil.Backup(destPath);

        WarnAboutKrak(compression, bnd.Files.Count);

        if (WarnAboutZstd(compression))
        {
            bnd.Compression = DCX.Type.DCX_DFLT_11000_44_9;
        }

        bnd.Write(destPath);
    }
}