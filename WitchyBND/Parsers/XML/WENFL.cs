﻿using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WENFL : WXMLParser
{

    public override string Name => "ENFL";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<ENFL>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path).ToLower();
        return filename.EndsWith(".entryfilelist");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var enfl = (file as ENFL)!;

        XDocument xDoc = new XDocument();

        using (var xmlWriter = xDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(ENFL));
            thing.Serialize(xmlWriter, enfl);
        }

        var destPath = GetUnpackDestPath(srcPath, recursive);
        AddLocationToXml(srcPath, recursive, xDoc.Root!);
        xDoc.Save(destPath);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        XElement xml = LoadXml(srcPath);

        XmlSerializer serializer = new XmlSerializer(typeof(ENFL));
        XmlReader xmlReader = xml.CreateReader();

        var enfl = (ENFL)serializer.Deserialize(xmlReader);
        if (enfl == null)
            throw new Exception();

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        enfl.Write(outPath);
    }
}