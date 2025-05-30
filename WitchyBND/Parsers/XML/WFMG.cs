﻿using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFMG : WXMLParser
{

    public override string Name => "FMG";

    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        return Path.GetExtension(path).ToLower() == ".fmg";
    }

    public override bool? IsSimple(string path)
    {
        return Is(path, null, out _);
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        FMG fmg = FMG.Read(srcPath);
        var xml = PrepareXmlManifest(srcPath, recursive, false, fmg.Compression, out XDocument xDoc, null);
        
        xml.Add(
            new XElement("version", fmg.Version.ToString()), 
            new XElement("bigendian", fmg.BigEndian.ToString()));

        var entries = new XElement("entries");
        
        fmg.Entries.Sort((e1, e2) => e1.ID.CompareTo(e2.ID));
        
        foreach (FMG.Entry entry in fmg.Entries)
        {
            var entryXml = new XElement("text", new XAttribute("id", entry.ID.ToString()), entry.Text ?? "%null%");
            entries.Add(entryXml);
        }
        
        xml.Add(entries);
        
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        FMG fmg = new FMG();

        XElement xml = LoadXml(srcPath);
        fmg.Compression = ReadCompressionInfoFromXml(xml);

        fmg.Version = (FMG.FMGVersion)Enum.Parse(typeof(FMG.FMGVersion), xml.Element("version")!.Value);
        fmg.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);

        foreach (XElement textNode in xml.Element("entries")!.Elements("text"))
        {
            int id = int.Parse(textNode.Attribute("id")!.Value);
            // \r\n is drawn as two newlines ingame
            string? text = textNode.Value.Replace("\r", "");
            if (text == "%null%")
                text = null;
            fmg.Entries.Add(new FMG.Entry(id, text));
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        fmg.TryWriteSoulsFile(outPath);
    }
}