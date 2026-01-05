using System;
using System.IO;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMATBIN : WSerializedXMLParser
{
    public override string Name => "MATBIN";
    
    public override int Version => WBUtil.WitchyVersionToInt("2.15.0.0");

    public override Type SerializedType => typeof(MATBIN);

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MATBIN>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = OSPath.GetFileName(path).ToLower();
        return filename.EndsWith(".matbin") || filename.EndsWith(".matbin.dcx");
    }

    public override bool IsUnpacked(string path)
    {
        if (OSPath.GetExtension(path).ToLower() != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name == Name;
    }
}