using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND;

/**
 * Code mostly adapted from FXR3-XMLR.
 */
static class WFXR
{
    public static bool Unpack(this Fxr3 fxr, string sourceFile)
    {
        XDocument XDoc = new XDocument();

        using (var xmlWriter = XDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(Fxr3));
            thing.Serialize(xmlWriter, fxr);
        }

        XDoc.Save($"{sourceFile}.xml");

        return false;
    }

    public static bool Repack(string sourceFile)
    {
        XDocument XML = XDocument.Load(sourceFile);
        XmlSerializer test = new XmlSerializer(typeof(Fxr3));
        XmlReader xmlReader = XML.CreateReader();

        var fxr = (Fxr3)test.Deserialize(xmlReader);

        string outPath = sourceFile.Replace(".fxr.xml", ".fxr");
        WBUtil.Backup(outPath);
        fxr.TryWriteSoulsFile(outPath);

        return false;
    }
}