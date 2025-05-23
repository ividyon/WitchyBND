using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;
using WitchyBND.Services;
using WitchyLib;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyBND.Parsers;

public enum WFileParserVerb
{
    None = 0,
    Unpack = 1,
    Serialize = 2,
}

public abstract class WFileParser
{
    protected readonly IGameService gameService;
    protected readonly IErrorService errorService;
    protected readonly IOutputService output;

    public WFileParser()
    {
        gameService = ServiceProvider.GetService<IGameService>();
        errorService = ServiceProvider.GetService<IErrorService>();
        output = ServiceProvider.GetService<IOutputService>();
    }

    public virtual WFileParserVerb Verb => WFileParserVerb.Unpack;
    public virtual bool IncludeInList => true;
    public virtual bool AppliesRecursively => true;
    public abstract string Name { get; }
    public virtual string ListName => Name;

    public virtual int Version => 0;
    public virtual string VersionAttributeName => "WitchyVersion";
    public virtual string XmlTag
    {
        get
        {
            List<char> chars = new();
            foreach (char c in Name.ToLower())
            {
                if (char.IsAsciiDigit(c) || char.IsAsciiLetter(c))
                {
                    chars.Add(c);
                    continue;
                }
                try
                {
                    chars.Add(XmlConvert.VerifyName(c.ToString()).ToCharArray().First());
                }
                catch
                {
                    chars.Add('_');
                }
            }

            return new string(chars.ToArray());
        }
    }

    public virtual bool HasPreprocess => false;

    public virtual bool Preprocess(string srcPath, bool recursive, ref Dictionary<string, (WFileParser, ISoulsFile)> files)
    {
        return false;
    }

    public abstract bool Is(string path, byte[]? data, out ISoulsFile? file);
    public abstract bool? IsSimple(string path);

    public virtual bool IsSimpleFirst(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return IsSimple(path) ?? Is(path, data, out file);
    }

    public abstract bool Exists(string path);
    public abstract bool ExistsUnpacked(string path);
    public abstract bool IsUnpacked(string path);

    public abstract string GetUnpackDestPath(string srcPath, bool recursive);

    public virtual void WriteXmlManifest(XDocument xDoc, string srcPath, bool recursive)
    {
        var destPath = GetUnpackDestPath(srcPath, recursive);
        xDoc.Save(destPath);
    }
    public virtual int GetUnpackedVersion(string path)
    {
        var doc = XDocument.Load(path);
        var attr = doc.Root?.Attribute(VersionAttributeName);
        if (attr == null) return 0;
        return int.Parse(attr.Value);
    }

    public virtual bool UnpackedFitsVersion(string path)
    {
        if (Version == 0) return true;
        if (GetUnpackedVersion(path) < Version)
            return false;
        return true;
    }
    public abstract void Unpack(string srcPath, ISoulsFile? file, bool recursive);
    public abstract void Repack(string srcPath, bool recursive);
    public static void AddLocationToXml(string path, string srcPath, bool recursive)
    {
        XDocument xml = XDocument.Load(path);
        AddLocationToXml(srcPath, recursive, xml.Root!);
        xml.Save(path);
    }

    public static XElement RemoveLocationFromXml(XElement xml)
    {
        xml.Element("filename")?.Remove();
        xml.Element("sourcePath")?.Remove();
        return xml;
    }

    public static void AddLocationToXml(string path, bool recursive, XmlWriter xw, bool skipFilename = false)
    {
        string? location = Configuration.Active.Location;
        if (!string.IsNullOrEmpty(location) && !recursive)
        {
            string srcPath = Path.GetDirectoryName(path)!;
            if (location.StartsWith(srcPath))
                srcPath = Path.GetRelativePath(location, srcPath);
            xw.WriteElementString("sourcePath", srcPath);
        }
        if (!skipFilename)
            xw.WriteElementString("filename",  Path.GetFileName(path));
    }
    public static void AddLocationToXml(string path, bool recursive, XElement xml, bool skipFilename = false)
    {
        string? location = Configuration.Active.Location;
        if (!string.IsNullOrEmpty(location) && !recursive)
        {
            string srcPath = Path.GetDirectoryName(path)!;
            if (location.StartsWith(srcPath))
                srcPath = Path.GetRelativePath(location, srcPath);
            xml.AddFirst(new XElement("sourcePath", srcPath));
        }
        if (!skipFilename)
            xml.AddFirst(new XElement("filename", Path.GetFileName(path)));
    }
    
    public static DCX.CompressionData ReadCompressionDataFromXml(XElement xml)
    {
        string typeString = xml.Element("compression")?.Value ?? "None";
        // Legacy support
        switch (typeString.ToUpper())
        {
            case "DCX_DFLT_10000_24_9":
                return new DCX.DcxDfltCompressionData(0x10000, 0x24, 0x2C, 9, 0);
            case "DCX_DFLT_10000_44_9":
                return new DCX.DcxDfltCompressionData(0x10000, 0x44, 0x4C, 9, 0);
            case "DCX_DFLT_11000_44_8":
                return new DCX.DcxDfltCompressionData(0x11000, 0x44, 0x4C, 8, 0);
            case "DCX_DFLT_11000_44_9":
                return new DCX.DcxDfltCompressionData(0x11000, 0x44, 0x4C, 9, 0);
            case "DCX_DFLT_11000_44_9_15":
                return new DCX.DcxDfltCompressionData(0x11000, 0x44, 0x4C, 9, 15);
            case "DCX_KRAK_MAX":
            case "DCX_KRAK_9":
                return new DCX.DcxKrakCompressionData(9);
        }
        DCX.Type type = Enum.Parse<DCX.Type>(typeString);
        switch (type)
        {
            case DCX.Type.Unknown:
                return new DCX.UnkCompressionData();
            case DCX.Type.None:
                return new DCX.NoCompressionData();
            case DCX.Type.Zlib:
                return new DCX.ZlibCompressionData();
            case DCX.Type.DCP_EDGE:
                return new DCX.DcpEdgeCompressionData();
            case DCX.Type.DCP_DFLT:
                return new DCX.DcpDfltCompressionData();
            case DCX.Type.DCX_EDGE:
                return new DCX.DcxEdgeCompressionData();
            case DCX.Type.DCX_DFLT:
                // DCX_DFLT_11000_44_9_15
                return new DCX.DcxDfltCompressionData(
                    int.Parse(xml.Element("dfltUnk04")?.Value ?? ((int)0x11000).ToString()),
                    int.Parse(xml.Element("dfltUnk10")?.Value ?? ((int)0x44).ToString()),
                    int.Parse(xml.Element("dfltUnk14")?.Value ?? ((int)0x4C).ToString()),
                    byte.Parse(xml.Element("dfltUnk30")?.Value ?? ((byte)0x9).ToString()),
                    byte.Parse(xml.Element("dfltUnk38")?.Value ?? ((byte)0x15).ToString())
                );
            case DCX.Type.DCX_KRAK:
                return new DCX.DcxKrakCompressionData(byte.Parse(xml.Element("compressionLevel")!.Value));
            case DCX.Type.DCX_ZSTD:
                return new DCX.DcxZstdCompressionData();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void WriteCompressionDataToXml(XElement xml, DCX.CompressionData? compression)
    {
        compression ??= new DCX.NoCompressionData();
        xml.Add(new XElement("compression", compression.Type.ToString()));
        switch (compression)
        {
            case DCX.DcxDfltCompressionData dfltCompression:
                xml.Add(new XElement("dfltUnk04", dfltCompression.Unk04.ToString()));
                xml.Add(new XElement("dfltUnk10", dfltCompression.Unk10.ToString()));
                xml.Add(new XElement("dfltUnk14", dfltCompression.Unk14.ToString()));
                xml.Add(new XElement("dfltUnk30", dfltCompression.Unk30.ToString()));
                xml.Add(new XElement("dfltUnk38", dfltCompression.Unk38.ToString()));
                break;
            case DCX.DcxKrakCompressionData krakCompression:
                xml.Add(new XElement("compressionLevel", krakCompression.CompressionLevel.ToString()));
                break;
        }
    }

    public virtual XElement PrepareXmlManifest(string srcPath, bool recursive, bool skipFilename, DCX.CompressionData? compression, out XDocument xDoc, string? root)
    {
        xDoc = new XDocument();
        var xml = new XElement(XmlTag);
        if (Version > 0) xml.Add(new XAttribute(VersionAttributeName, Version.ToString()));
        WriteCompressionDataToXml(xml, compression);
        AddLocationToXml(srcPath, recursive, xml, skipFilename);
        if (!string.IsNullOrEmpty(root))
            xml.Add(new XElement("root", root));
        xDoc.Add(xml);
        return xml;
    }
    
    public static XElement LoadXml(string path)
    {
        XDocument doc = XDocument.Load(path);
        if (doc.Root == null) throw new XmlException("XML has no root");
        return doc.Root;
    }

    public static void Backup(string path)
    {
        Configuration.Active.GitBackup = false;
        if (Configuration.Active.BackupMethod == WBUtil.BackupMethod.None) return;
        if (!Configuration.Active.GitBackup && WBUtil.IsInGit(path)) return;
        WBUtil.Backup(path, Configuration.Active.BackupMethod);
    }

    private static bool IsRead<TFormat>(string path, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        if (SoulsFile<TFormat>.IsRead(path, out TFormat format))
        {
            file = format;
            return true;
        }

        file = null;
        return false;
    }

    private static bool IsRead<TFormat>(byte[] data, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        if (SoulsFile<TFormat>.IsRead(data, out TFormat format))
        {
            file = format;
            return true;
        }

        file = null;
        return false;
    }

    public static bool IsRead<TFormat>(string path, byte[]? data, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        try
        {
            return data != null ? IsRead<TFormat>(data, out file) : IsRead<TFormat>(path, out file);
        }
        catch (Exception)
        {
            file = null;
            return false;
        }
    }
}

public abstract class WSingleFileParser : WFileParser
{
    public abstract string GetRepackDestPath(string srcPath, XElement xml);
    public override bool Exists(string path)
    {
        return File.Exists(path);
    }
    public override bool ExistsUnpacked(string path)
    {
        return File.Exists(path);
    }
}

public abstract class WDeferredFileParser : WSingleFileParser
{
    public abstract string[] UnpackExtensions { get; }
    public abstract string[] RepackExtensions { get; }
    public abstract DeferFormat DeferFormat { get; }
    public override bool AppliesRecursively => false;
    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        var extension = WBUtil.GetFullExtensions(path).ToLower();
        var cond = UnpackExtensions.Contains(extension);
        if (cond && !Configuration.Active.DeferTools.ContainsKey(DeferFormat))
            throw new DeferToolPathException(DeferFormat);
        return cond;
    }

    public override bool? IsSimple(string path)
    {
        return Is(path, null, out _);
    }

    public override bool IsUnpacked(string path)
    {
        var extension = WBUtil.GetFullExtensions(path).ToLower();
        var cond = RepackExtensions.Contains(extension);
        if (cond && !Configuration.Active.DeferTools.ContainsKey(DeferFormat))
            throw new DeferToolPathException(DeferFormat);
        return cond && !string.IsNullOrWhiteSpace(Configuration.Active.DeferTools[DeferFormat].RepackArgs);
    }

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        throw new NotSupportedException();
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        DeferredFormatHandling.Unpack(DeferFormat, srcPath);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        DeferredFormatHandling.Repack(DeferFormat, srcPath);
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        throw new NotSupportedException();
    }
}

public abstract class WXMLParser : WSingleFileParser
{
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName!;
        string? location = Configuration.Active.Location;
        if (!string.IsNullOrEmpty(location) && !recursive)
            sourceDir = location;
        sourceDir = Path.GetFullPath(sourceDir);
        return Path.Combine(sourceDir, $"{Path.GetFileName(srcPath)}.xml");
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        var path = xml.Element("sourcePath")?.Value;
        if (path != null)
        {
            return $"{path}\\{Path.GetFileName(srcPath).Replace(".xml", "")}";
        }
        return srcPath.Replace(".xml", "");
    }

    public override bool IsUnpacked(string path)
    {
        if (Path.GetExtension(path) != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name.ToString().ToLower() == XmlTag.ToLower();
    }
}

public abstract class WSerializedXMLParser : WXMLParser
{
    public abstract Type SerializedType { get; }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var xml = PrepareXmlManifest(srcPath, recursive, false, file!.Compression, out XDocument _, null);

        var xDoc = new XDocument();
        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(SerializedType);
            thing.Serialize(xmlWriter, file);
        }
        
        xDoc.Root!.AddFirst(xml.Elements());

        xDoc.Save(GetUnpackDestPath(srcPath, recursive));
    }

    public override void Repack(string srcPath, bool recursive)
    {

        XElement xml = LoadXml(srcPath);

        XmlSerializer serializer = new XmlSerializer(SerializedType);
        XmlReader xmlReader = xml.CreateReader();

        object thing = serializer.Deserialize(xmlReader);
        if (thing.GetType() != SerializedType || thing is not ISoulsFile file)
            throw new Exception();

        file.Compression = ReadCompressionDataFromXml(xml);

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        file.Write(outPath);
    }
}