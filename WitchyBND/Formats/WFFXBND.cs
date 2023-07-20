using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SoulsFormats;
using WitchyLib;
using TPF = WitchyFormats.TPF;

namespace WitchyBND;

static class WFFXBND
{
    public static bool Is(string filepath)
    {
        var filename = Path.GetFileName(filepath);
        return (filename.EndsWith(".ffxbnd") || filename.EndsWith(".ffxbnd.dcx")) && BND4.Is(filepath);
    }
    public static bool Is(byte[] bytes, string filename)
    {
        return (filename.EndsWith(".ffxbnd") || filename.EndsWith(".ffxbnd.dcx")) && BND4.Is(bytes);
    }
    public static void UnpackFFXBND(this BND4Reader bnd, string sourceName, string targetDir, IProgress<float> progress)
    {
        Directory.CreateDirectory(targetDir);
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{targetDir}\\_witchy-ffxbnd.xml", xws);
        xw.WriteStartElement("ffxbnd");
        xw.WriteElementString("filename", sourceName);
        xw.WriteElementString("compression", bnd.Compression.ToString());
        xw.WriteElementString("version", bnd.Version);
        xw.WriteElementString("format", bnd.Format.ToString());
        xw.WriteElementString("bigendian", bnd.BigEndian.ToString());
        xw.WriteElementString("bitbigendian", bnd.BitBigEndian.ToString());
        xw.WriteElementString("unicode", bnd.Unicode.ToString());
        xw.WriteElementString("extended", $"0x{bnd.Extended:X2}");
        xw.WriteElementString("unk04", bnd.Unk04.ToString());
        xw.WriteElementString("unk05", bnd.Unk05.ToString());
        xw.WriteEndElement();
        xw.Close();

        // Files
        var effectDir = $@"{targetDir}\effect";
        Directory.CreateDirectory(effectDir);
        var textureDir = $@"{targetDir}\texture";
        Directory.CreateDirectory(textureDir);
        var modelDir = $@"{targetDir}\model";
        Directory.CreateDirectory(modelDir);
        var animDir = $@"{targetDir}\animation";
        Directory.CreateDirectory(animDir);
        var resDir = $@"{targetDir}\resource";
        Directory.CreateDirectory(resDir);

        foreach (BinderFileHeader file in bnd.Files)
        {
            byte[] bytes = bnd.ReadFile(file);
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
                    fileTargetDir = $@"{targetDir}\other";
                    Directory.CreateDirectory(fileTargetDir);
                    break;
            }
            File.WriteAllBytes($@"{fileTargetDir}/{fileTargetName}", bytes);
        }
    }

    public static void Repack(string sourceDir, string targetDir)
    {

        var bnd = new BND4();
        var xml = new XmlDocument();

        xml.Load(WBUtil.GetXmlPath("ffxbnd", sourceDir));

        string filename = xml.SelectSingleNode("ffxbnd/filename").InnerText;

        Enum.TryParse(xml.SelectSingleNode("ffxbnd/compression")?.InnerText ?? "None", out DCX.Type compression);
        bnd.Compression = compression;

        bnd.Version = xml.SelectSingleNode("ffxbnd/version").InnerText;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("ffxbnd/format").InnerText);
        bnd.BigEndian = bool.Parse(xml.SelectSingleNode("ffxbnd/bigendian").InnerText);
        bnd.BitBigEndian = bool.Parse(xml.SelectSingleNode("ffxbnd/bitbigendian").InnerText);
        bnd.Unicode = bool.Parse(xml.SelectSingleNode("ffxbnd/unicode").InnerText);
        bnd.Extended = Convert.ToByte(xml.SelectSingleNode("ffxbnd/extended").InnerText, 16);
        bnd.Unk04 = bool.Parse(xml.SelectSingleNode("ffxbnd/unk04").InnerText);
        bnd.Unk05 = bool.Parse(xml.SelectSingleNode("ffxbnd/unk05").InnerText);

        // Files
        var effectDir = $@"{sourceDir}\effect";
        var textureDir = $@"{sourceDir}\texture";
        var modelDir = $@"{sourceDir}\model";
        var animDir = $@"{sourceDir}\animation";
        var resDir = $@"{sourceDir}\resource";

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

        string outPath = $"{targetDir}\\{filename}";
        WBUtil.Backup(outPath);
        bnd.Write(outPath);
    }
}