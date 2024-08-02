using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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
    public abstract bool Exists(string path);
    public abstract bool ExistsUnpacked(string path);
    public abstract bool IsUnpacked(string path);

    public abstract string GetUnpackDestPath(string srcPath, bool recursive);

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
    public static XElement LoadXml(string path)
    {
        XDocument doc = XDocument.Load(path);
        if (doc.Root == null) throw new XmlException("XML has no root");
        return doc.Root;
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
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        var extension = WBUtil.GetFullExtensions(path).ToLower();
        var cond = UnpackExtensions.Contains(extension);
        if (cond && !Configuration.Active.DeferTools.ContainsKey(DeferFormat))
            throw new DeferToolPathException(DeferFormat);
        return cond;
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