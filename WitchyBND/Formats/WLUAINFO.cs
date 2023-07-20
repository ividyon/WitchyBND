using SoulsFormats;
using System.Xml;
using WitchyLib;

namespace WitchyBND
{
    static class WLUAINFO
    {
        public static void Unpack(this LUAINFO info, string sourceFile)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            XmlWriter xw = XmlWriter.Create($"{sourceFile}.xml", xws);
            xw.WriteStartElement("luainfo");
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

        public static void Repack(string sourceFile)
        {
            LUAINFO info = new LUAINFO();
            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);
            info.BigEndian = bool.Parse(xml.SelectSingleNode("luainfo/bigendian").InnerText);
            info.LongFormat = bool.Parse(xml.SelectSingleNode("luainfo/longformat").InnerText);

            foreach (XmlNode node in xml.SelectNodes("luainfo/goals/goal"))
            {
                int id = int.Parse(node.Attributes["id"].InnerText);
                string name = node.SelectSingleNode("name").InnerText;
                bool battleInterrupt = bool.Parse(node.SelectSingleNode("battleinterrupt").InnerText);
                bool logicInterrupt = bool.Parse(node.SelectSingleNode("logicinterrupt").InnerText);
                string logicInterruptName = node.SelectSingleNode("logicinterruptname")?.InnerText;
                info.Goals.Add(new LUAINFO.Goal(id, name, battleInterrupt, logicInterrupt, logicInterruptName));
            }

            string outPath = sourceFile.Replace(".luainfo.xml", ".luainfo");
            WBUtil.Backup(outPath);
            info.Write(outPath);
        }
    }
}
