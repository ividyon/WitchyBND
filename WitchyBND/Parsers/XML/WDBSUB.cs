using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using SoulsFormats.Other.AC4;
using WitchyLib;

namespace WitchyBND.Parsers
{
    public class WDBSUB : WXMLParser
    {
        public override string Name => nameof(DBSUB);
        public override string XmlTag => nameof(DBSUB);

        public override bool Is(string path, byte[]? data, out ISoulsFile? file)
        {
            file = null;
            return (IsSimple(path) ?? false) && IsRead<DBSUB>(path, data, out file);
        }

        public override bool? IsSimple(string path)
        {
            var filename = OSPath.GetFileName(path).ToLower();
            return OSPath.GetExtension(filename) == ".bin" && (filename.StartsWith("chapter_") ||
                                                             filename.EndsWith("_b0.bin") ||
                                                             filename.EndsWith("_d0.bin"));
        }

        public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
        {
            var dbs = (file as DBSUB)!;
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.NewLineHandling = NewLineHandling.None; // Prevent \n from being turned into \r\n automatically
            xws.NewLineChars = "\n"; // Use only \n as all files here appear to use them
            xws.Encoding = Encoding.Unicode; // Ensure UTF16 files have the full range avaliable to them
            XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath, recursive), xws);
            xw.WriteStartElement(XmlTag);
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

        public override void Repack(string srcPath, bool recursive)
        {
            DBSUB dbs = new DBSUB();
            XElement xml = LoadXml(srcPath);

            dbs.Compression = ReadCompressionInfoFromXml(xml);

            uint.TryParse(xml.Element(nameof(dbs.EventID))?.Value ?? "0", out uint eventId);
            dbs.EventID = eventId;

            bool.TryParse(xml.Element(nameof(dbs.Unicode))?.Value ?? "false", out bool unicode);
            dbs.Unicode = unicode;

            XElement? subsNode = xml.Element($"{nameof(dbs.SubtitleEntries)}");
            if (subsNode != null)
            {
                foreach (XElement subNode in subsNode.Elements(nameof(DBSUB.SubtitleEntry)))
                {
                    var subEntry = new DBSUB.SubtitleEntry();

                    subEntry.FrameDelay = short.Parse(subNode.Element(nameof(subEntry.FrameDelay))?.Value
                        ?? throw new FriendlyException($"{nameof(subEntry.FrameDelay)} could not be parsed."));

                    subEntry.FrameTime = short.Parse(subNode.Element(nameof(subEntry.FrameTime))?.Value
                        ?? throw new FriendlyException($"{nameof(subEntry.FrameTime)} could not be parsed."));

                    subEntry.Text = subNode.Element(nameof(subEntry.Text))!.Value
                        ?? throw new FriendlyException($"{nameof(subEntry.Text)} could not be parsed.");

                    dbs.SubtitleEntries.Add(subEntry);
                }
            }

            XElement? vidsNode = xml.Element(nameof(dbs.VideoEntries));
            if (vidsNode != null)
            {
                foreach (XElement vidNode in vidsNode.Elements(nameof(DBSUB.VideoEntry)))
                {
                    var vidEntry = new DBSUB.VideoEntry();

                    vidEntry.Unk08 = short.Parse(vidNode.Element(nameof(vidEntry.Unk08))?.Value
                        ?? throw new FriendlyException($"{nameof(vidEntry.Unk08)} could not be parsed."));

                    vidEntry.Unk0A = short.Parse(vidNode.Element(nameof(vidEntry.Unk0A))?.Value
                        ?? throw new FriendlyException($"{nameof(vidEntry.Unk0A)} could not be parsed."));

                    vidEntry.Width = short.Parse(vidNode.Element(nameof(vidEntry.Width))?.Value
                        ?? throw new FriendlyException($"{nameof(vidEntry.Width)} could not be parsed."));

                    vidEntry.Height = short.Parse(vidNode.Element(nameof(vidEntry.Height))?.Value
                        ?? throw new FriendlyException($"{nameof(vidEntry.Height)} could not be parsed."));

                    vidEntry.Name = vidNode.Element(nameof(vidEntry.Name))!.Value
                        ?? throw new FriendlyException($"{nameof(vidEntry.Name)} could not be parsed.");

                    dbs.VideoEntries.Add(vidEntry);
                }
            }

            string outPath = srcPath.Replace(".xml", "");
            Backup(outPath);
            dbs.Write(outPath);
        }
    }
}
