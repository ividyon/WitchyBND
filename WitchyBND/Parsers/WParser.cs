using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using SoulsFormats;
using WitchyBND.CliModes;
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
    public abstract string Name { get; }
    public virtual string ListName => Name;

    public virtual int Version => 0;
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

    protected readonly HashSet<string> PreprocessedPaths = new();

    public virtual bool Preprocess(string srcPath)
    { return false; }

    public abstract bool Is(string path, byte[]? data, out ISoulsFile? file);
    public abstract bool Exists(string path);
    public abstract bool ExistsUnpacked(string path);
    public abstract bool IsUnpacked(string path);

    public abstract string GetUnpackDestPath(string srcPath);

    public virtual int GetUnpackedVersion(string path)
    {
        return Version;
    }
    public virtual bool UnpackedFitsVersion(string path)
    {
        if (Version == 0) return true;
        if (GetUnpackedVersion(path) < Version)
            return false;
        return true;
    }

    public abstract void Unpack(string srcPath, ISoulsFile? file);
    public abstract void Repack(string srcPath);
    public static void AddLocationToXml(string path, string srcPath)
    {
        XDocument xml = XDocument.Load(path);
        AddLocationToXml(srcPath, xml.Root!);
        xml.Save(path);
    }

    public static XElement RemoveLocationFromXml(XElement xml)
    {
        xml.Element("filename")?.Remove();
        xml.Element("sourcePath")?.Remove();
        return xml;
    }
    public static void AddLocationToXml(string path, XElement xml)
    {
        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            xml.AddFirst(new XElement("sourcePath", Path.GetDirectoryName(path)));
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
        return data != null ? IsRead<TFormat>(data, out file) : IsRead<TFormat>(path, out file);
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

public abstract class WXMLParser : WSingleFileParser
{
    public virtual string VersionAttributeName => "WitchyVersion";
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;

    public override string GetUnpackDestPath(string srcPath)
    {
        if (string.IsNullOrEmpty(Configuration.Args.Location))
            return $"{srcPath}.xml";
        return $"{Configuration.Args.Location}\\{Path.GetFileName(srcPath)}.xml";
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