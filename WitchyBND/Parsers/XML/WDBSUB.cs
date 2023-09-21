using SoulsFormats;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers
{
    public class WDBSUB : WXMLParser
    {
        public override string Name => nameof(DBSUB);

        public override bool Is(string path)
        {
            return DBSUB.Is(path);
        }

        public override bool IsUnpacked(string path)
        {
            if (Path.GetExtension(path) != ".xml")
                return false;

            var doc = XDocument.Load(path);
            return doc.Root != null && doc.Root.Name == Name;
        }

        public override void Unpack(string srcPath)
        {
            DBSUB dbs = DBSUB.Read(srcPath);
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.NewLineHandling = NewLineHandling.None; // Prevent \n from being turned into \r\n automatically
            XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath), xws);
            xw.WriteStartElement(Name);
            xw.WriteElementString(nameof(dbs.Compression), dbs.Compression.ToString());
            xw.WriteElementString(nameof(dbs.EventID), dbs.EventID.ToString());
            xw.WriteElementString(nameof(dbs.Unicode), dbs.Unicode.ToString());

            if (dbs.SubtitleEntries.Count > 0)
            {
                xw.WriteStartElement(nameof(dbs.SubtitleEntries));
                foreach (DBSUB.SubtitleEntry sub in dbs.SubtitleEntries)
                {
                    xw.WriteStartElement(nameof(DBSUB.SubtitleEntry));
                    xw.WriteElementString(nameof(sub.FrameDelay), sub.FrameDelay.ToString());
                    xw.WriteElementString(nameof(sub.FrameTime), sub.FrameTime.ToString());
                    xw.WriteElementString(nameof(sub.Text), sub.Text);
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
            }
            if (dbs.VideoEntries.Count > 0)
            {
                xw.WriteStartElement(nameof(dbs.VideoEntries));
                foreach (DBSUB.VideoEntry vid in dbs.VideoEntries)
                {
                    xw.WriteStartElement(nameof(DBSUB.VideoEntry));
                    xw.WriteElementString(nameof(vid.Unk08), vid.Unk08.ToString());
                    xw.WriteElementString(nameof(vid.Unk0A), vid.Unk0A.ToString());
                    xw.WriteElementString(nameof(vid.Width), vid.Width.ToString());
                    xw.WriteElementString(nameof(vid.Height), vid.Height.ToString());
                    xw.WriteElementString(nameof(vid.Name), vid.Name);
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.Close();
        }

        public override void Repack(string srcPath)
        {
            DBSUB dbs = new DBSUB();

            XmlDocument xml = new XmlDocument();
            xml.Load(srcPath);

            Enum.TryParse(xml.SelectSingleNode($"{Name}/{nameof(dbs.Compression)}")?.InnerText ?? "None", out DCX.Type compression);
            dbs.Compression = compression;

            uint.TryParse(xml.SelectSingleNode($"{Name}/{nameof(dbs.EventID)}")?.InnerText ?? "0", out uint eventID);
            dbs.EventID = eventID;

            bool.TryParse(xml.SelectSingleNode($"{Name}/{nameof(dbs.Unicode)}")?.InnerText ?? "false", out bool unicode);
            dbs.Unicode = unicode;

            XmlNode subsNode = xml.SelectSingleNode($"{Name}/{nameof(dbs.SubtitleEntries)}");
            if (subsNode != null)
            {
                foreach (XmlNode subNode in subsNode.SelectNodes(nameof(DBSUB.SubtitleEntry)))
                {
                    var subEntry = new DBSUB.SubtitleEntry();

                    subEntry.FrameDelay = short.Parse(subNode.SelectSingleNode(nameof(subEntry.FrameDelay))?.InnerText
                        ?? throw new FriendlyException($"{nameof(subEntry.FrameDelay)} could not be parsed."));

                    subEntry.FrameTime = short.Parse(subNode.SelectSingleNode(nameof(subEntry.FrameTime))?.InnerText
                        ?? throw new FriendlyException($"{nameof(subEntry.FrameTime)} could not be parsed."));

                    subEntry.Text = subNode.SelectSingleNode(nameof(subEntry.Text)).InnerText
                        ?? throw new FriendlyException($"{nameof(subEntry.Text)} could not be parsed.");

                    dbs.SubtitleEntries.Add(subEntry);
                }
            }

            XmlNode vidsNode = xml.SelectSingleNode($"{Name}/{nameof(dbs.VideoEntries)}");
            if (vidsNode != null)
            {
                foreach (XmlNode vidNode in vidsNode.SelectNodes(nameof(DBSUB.VideoEntry)))
                {
                    var vidEntry = new DBSUB.VideoEntry();

                    vidEntry.Unk08 = short.Parse(vidNode.SelectSingleNode(nameof(vidEntry.Unk08))?.InnerText
                        ?? throw new FriendlyException($"{nameof(vidEntry.Unk08)} could not be parsed."));

                    vidEntry.Unk0A = short.Parse(vidNode.SelectSingleNode(nameof(vidEntry.Unk0A))?.InnerText
                        ?? throw new FriendlyException($"{nameof(vidEntry.Unk0A)} could not be parsed."));

                    vidEntry.Width = short.Parse(vidNode.SelectSingleNode(nameof(vidEntry.Width))?.InnerText
                        ?? throw new FriendlyException($"{nameof(vidEntry.Width)} could not be parsed."));

                    vidEntry.Height = short.Parse(vidNode.SelectSingleNode(nameof(vidEntry.Height))?.InnerText
                        ?? throw new FriendlyException($"{nameof(vidEntry.Height)} could not be parsed."));

                    vidEntry.Name = vidNode.SelectSingleNode(nameof(vidEntry.Name)).InnerText
                        ?? throw new FriendlyException($"{nameof(vidEntry.Name)} could not be parsed.");

                    dbs.VideoEntries.Add(vidEntry);
                }
            }

            string outPath = srcPath.Replace(".xml", "");
            WBUtil.Backup(outPath);
            dbs.TryWriteSoulsFile(outPath);
        }
    }
}
