using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WLUAINFO : WXMLParser
{

    public override string Name => "LUAINFO";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<LUAINFO>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path);
        return filename.EndsWith(".luainfo");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var info = (file as LUAINFO)!;
        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath, recursive), xws);
        xw.WriteStartElement("luainfo");

        AddLocationToXml(srcPath, recursive, xw);

        xw.WriteElementString("bigendian", info.BigEndian.ToString());
        xw.WriteElementString("longformat", info.LongFormat.ToString());
        xw.WriteStartElement("goals");

        foreach (LUAINFO.Goal goal in info.Goals)
        {
            xw.WriteStartElement("goal");
            xw.WriteAttributeString("id", goal.ID.ToString());
            xw.WriteElementString("name", goal.Name);
            xw.WriteElementString("battleinterrupt", goal.BattleInterrupt.ToString());
            xw.WriteElementString("logicinterrupt", goal.LogicInterrupt.ToString());
            if (goal.LogicInterruptName != null)
                xw.WriteElementString("logicinterruptname", goal.LogicInterruptName);
            xw.WriteEndElement();
        }

        xw.WriteEndElement();
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        LUAINFO info = new LUAINFO();
        XElement xml = LoadXml(srcPath);
        info.BigEndian = bool.Parse(xml.Element("bigendian").Value);
        info.LongFormat = bool.Parse(xml.Element("longformat").Value);

        foreach (XElement node in xml.Element("goals")?.Elements("goal") ?? new XElement[]{})
        {
            int id = int.Parse(node.Attribute("id").Value);
            string name = node.Element("name").Value;
            bool battleInterrupt = bool.Parse(node.Element("battleinterrupt").Value);
            bool logicInterrupt = bool.Parse(node.Element("logicinterrupt").Value);
            string logicInterruptName = node.Element("logicinterruptname")?.Value;
            info.Goals.Add(new LUAINFO.Goal(id, name, battleInterrupt, logicInterrupt, logicInterruptName));
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        info.TryWriteSoulsFile(outPath);
    }
}