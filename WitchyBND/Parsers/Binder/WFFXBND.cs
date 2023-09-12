using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFFXBND : WBinderParser
{

    public override string Name => "FFXBND";
    public override bool Is(string path)
    {
        return Configuration.Bnd && File.Exists(path) && (path.EndsWith(".ffxbnd") || path.EndsWith(".ffxbnd.dcx")) && BND4.Is(path);
    }

    public override void Unpack(string srcPath)
    {
        var destDir = GetUnpackDestDir(srcPath);
        var srcName = Path.GetFileName(srcPath);
        var bnd = BND4.Read(srcPath);
        Directory.CreateDirectory(destDir);
        var xml = new XElement("ffxbnd",
            new XElement("filename", srcName),
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

        using var xw = XmlWriter.Create($"{destDir}\\{GetBinderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true,
        });
        xml.WriteTo(xw);
        xw.Close();

        // Files
        var effectDir = $@"{destDir}\effect";
        Directory.CreateDirectory(effectDir);
        var textureDir = $@"{destDir}\texture";
        Directory.CreateDirectory(textureDir);
        var modelDir = $@"{destDir}\model";
        Directory.CreateDirectory(modelDir);
        var animDir = $@"{destDir}\animation";
        Directory.CreateDirectory(animDir);
        var resDir = $@"{destDir}\resource";
        Directory.CreateDirectory(resDir);

        foreach (BinderFile file in bnd.Files)
        {
            byte[] bytes = file.Bytes;
            string fileTargetDir;
            string fileTargetName = Path.GetFileName(file.Name);
            switch (Path.GetExtension(file.Name))
            {
                case ".fxr":
                    fileTargetDir = effectDir;
                    break;
                case ".tpf":
                    var tpf = TPF.Read(bytes);
                    bytes = tpf.Textures[0].Bytes;
                    fileTargetName = $"{Path.GetFileName(tpf.Textures[0].Name)}.dds";
                    fileTargetDir = textureDir;
                    break;
                case ".flver":
                    fileTargetDir = modelDir;
                    break;
                case ".anibnd":
                    fileTargetDir = animDir;
                    break;
                case ".ffxreslist":
                    fileTargetDir = resDir;
                    break;
                default:
                    fileTargetDir = $@"{destDir}\other";
                    Directory.CreateDirectory(fileTargetDir);
                    break;
            }
            File.WriteAllBytes($@"{fileTargetDir}/{fileTargetName}", bytes);
        }
    }

    public override void Repack(string srcPath)
    {
        var bnd = new BND4();

        var doc = XDocument.Load(GetBinderXmlPath(srcPath));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        string filename = xml.Element("filename")!.Value;

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

        // Files
        var effectDir = $@"{srcPath}\effect";
        var textureDir = $@"{srcPath}\texture";
        var modelDir = $@"{srcPath}\model";
        var animDir = $@"{srcPath}\animation";
        var resDir = $@"{srcPath}\resource";

        var effectPaths = Directory.GetFiles(effectDir, "*.fxr").OrderBy(Path.GetFileName).ToList();
        var texturePaths = Directory.GetFiles(textureDir, "*.dds").OrderBy(Path.GetFileName).ToList();
        var modelPaths = Directory.GetFiles(modelDir, "*.flver").OrderBy(Path.GetFileName).ToList();
        var animPaths = Directory.GetFiles(animDir, "*.anibnd").OrderBy(Path.GetFileName).ToList();
        var resPaths = Directory.GetFiles(resDir, "*.ffxreslist").OrderBy(Path.GetFileName).ToList();

        // Sanity check fxr and reslist
        // Every FXR must have a matching reslist with the exact same name and vice versa
        // therefore there must also be the same amount of FXRs as reslists
        if (effectPaths.Count > 0 && resPaths.Count > 0)
        {
            var effectNames = new SortedSet<string>(effectPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            var resNames = new SortedSet<string>(resPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            if (!effectNames.SetEquals(resNames))
            {
                var diff1 = effectNames.Except(resNames).ToArray();
                var diff2 = effectNames.Except(resNames).ToArray();
                if (diff1.Any())
                {
                    throw new Exception($"Following FXRs are missing reslists: {string.Join(", ", diff1)}");
                }
                if (diff2.Any())
                {
                    throw new Exception($"Following reslists are missing FXRs: {string.Join(", ", diff2)}");
                }
            }
        }

        // Write files

        var rootPath = @"N:\GR\data\INTERROOT_win64\sfx\";

        for (int i = 0; i < effectPaths.Count; i++)
        {
            var filePath = effectPaths[i];
            var fileName = Path.GetFileName(filePath);
            var bytes = File.ReadAllBytes(filePath);
            var file = new BinderFile(Binder.FileFlags.Flag1, i, $@"{rootPath}\effect\{fileName}",
                bytes);
            bnd.Files.Add(file);
        }

        for (int i = 0; i < texturePaths.Count; i++)
        {
            var filePath = texturePaths[i];
            var fileName = Path.GetFileName(filePath);
            var bytes = File.ReadAllBytes(filePath);
            var tpf = new TPF();

            tpf.Compression = DCX.Type.None;
            tpf.Encoding = 0x01;
            tpf.Flag2 = 0x03;
            tpf.Platform = TPF.TPFPlatform.PC;

            var tex = new TPF.Texture();
            tex.Name = Path.GetFileNameWithoutExtension(fileName).Trim();
            tex.Format = 0;
            if (fileName.EndsWith("_m"))
            {
                tex.Format = 103;
            }
            else if (fileName.EndsWith("_n"))
            {
                tex.Format = 106;
            }
            tex.Bytes = bytes;

            tpf.Textures.Add(tex);

            bytes = tpf.Write();

            var file = new BinderFile(Binder.FileFlags.Flag1, 100000 + i, $@"{rootPath}\tex\{Path.ChangeExtension(fileName, "tpf")}",
                bytes);
            bnd.Files.Add(file);
        }

        for (int i = 0; i < modelPaths.Count; i++)
        {
            var filePath = modelPaths[i];
            var fileName = Path.GetFileName(filePath);
            var bytes = File.ReadAllBytes(filePath);
            var file = new BinderFile(Binder.FileFlags.Flag1, 200000 + i, $@"{rootPath}\model\{fileName}",
                bytes);
            bnd.Files.Add(file);
        }

        for (int i = 0; i < animPaths.Count; i++)
        {
            var filePath = animPaths[i];
            var fileName = Path.GetFileName(filePath);
            var bytes = File.ReadAllBytes(filePath);
            var file = new BinderFile(Binder.FileFlags.Flag1, 300000 + i, $@"{rootPath}\hkx\{fileName}",
                bytes);
            bnd.Files.Add(file);
        }

        for (int i = 0; i < resPaths.Count; i++)
        {
            var filePath = resPaths[i];
            var fileName = Path.GetFileName(filePath);
            var bytes = File.ReadAllBytes(filePath);
            var file = new BinderFile(Binder.FileFlags.Flag1, 400000 + i, $@"{rootPath}\ResourceList\{fileName}",
                bytes);
            bnd.Files.Add(file);
        }

        string destPath = GetRepackDestPath(srcPath, filename);
        WBUtil.Backup(destPath);
        bnd.Write(destPath);
    }

    public override string GetUnpackDestDir(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        string fileName = Path.GetFileName(srcPath);
        return $"{sourceDir}\\{fileName.Replace('.', '-')}-wffxbnd";
    }
}