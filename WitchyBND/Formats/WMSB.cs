using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using WitchyFormats;

namespace WitchyBND;

static class WMSB
{
    public static void Unpack(this MSBE msb, string sourceName, string targetDir, IProgress<float> progress)
    {
        Directory.CreateDirectory(targetDir);
        var partsDir = Path.Combine(targetDir, "parts");
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{targetDir}\\enemies.xml", xws);
        xw.WriteStartElement("enemies");
        msb.Parts.Enemies.ForEach(enemy => {
            var x = new DataContractSerializer(enemy.GetType());
            x.WriteObject(xw, enemy);
        });
        xw.WriteEndElement();
        xw.Close();
    }
}