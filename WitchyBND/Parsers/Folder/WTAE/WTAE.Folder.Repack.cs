using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WTAEFolder
{
    public override bool IsUnpacked(string path)
    {
        if (base.IsUnpacked(path))
        {
            return WarnAboutTAEs();
        }
        return false;
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var tae = new TAE();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        WBUtil.GameType game = GetGameTypeFromXml(xml);
        TAE.Template template = gameService.GetTAETemplate(game);

        tae.Compression = ReadCompressionInfoFromXml(xml);


        tae.ID = int.Parse(xml.Element("id")!.Value);
        tae.Format = Enum.Parse<TAE.TAEFormat>(xml.Element("format")!.Value);
        tae.EventBank = long.Parse(xml.Element("eventBank")!.Value);
        tae.SibName = xml.Element("sibName")!.Value;
        tae.SkeletonName = xml.Element("skeletonName")!.Value;
        tae.Flags = xml.Element("flags")!.Value.Split(",").Select(s => byte.Parse(s)).ToArray();
        tae.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);

        tae.Animations = new();

        var animFiles = Directory.GetFiles(srcPath, "anim-*.xml").Order().ToList();
        var bag = new ConcurrentBag<TAE.Animation>();

        void Callback(string file)
        {
            var anim = RepackAnim(file, tae, template);
            bag.Add(anim);
        }

        if (Configuration.Active.Parallel)
        {
            Parallel.ForEach(animFiles, Callback);
        }
        else
        {
            animFiles.ForEach(Callback);
        }

        tae.Animations = bag.OrderBy(a => a.ID).ToList();

        tae.ApplyTemplate(gameService.GetTAETemplate(game));

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        tae.Write(outPath);
    }

    private TAE.Animation RepackAnim(string file, TAE tae, TAE.Template template)
    {
            var id = int.Parse(new string(Path.GetFileNameWithoutExtension(file).Where(c => char.IsDigit(c)).ToArray()));
            var animXml = XDocument.Load(file).Root!;
            var animName = animXml.Element("name")!.Value;
            var headerEl = animXml.Element("header")!;
            var headerType = Enum.Parse<TAE.Animation.MiniHeaderType>(headerEl.Element("type")!.Value);
            TAE.Animation.AnimMiniHeader header;
            switch (headerType)
            {
                case TAE.Animation.MiniHeaderType.Standard:
                    var standard = new TAE.Animation.AnimMiniHeader.Standard();
                    standard.AllowDelayLoad = bool.Parse(headerEl.Element("allowDelayLoad")!.Value);
                    standard.ImportsHKX = bool.Parse(headerEl.Element("importsHkx")!.Value);
                    standard.IsLoopByDefault = bool.Parse(headerEl.Element("loopByDefault")!.Value);
                    standard.ImportHKXSourceAnimID = int.Parse(headerEl.Element("importHkxSourceAnimId")!.Value);
                    header = standard;
                    break;
                case TAE.Animation.MiniHeaderType.ImportOtherAnim:
                    var import = new TAE.Animation.AnimMiniHeader.ImportOtherAnim();
                    import.ImportFromAnimID = int.Parse(headerEl.Element("animId")!.Value);
                    header = import;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var anim = new TAE.Animation(id, header, animName);
            anim.Events = new();
            anim.EventGroups = new();

            foreach (XElement groupEl in animXml.Element("animGroups")!.Elements("animGroup"))
            {
                var type = long.Parse(groupEl.Element("type")!.Value);
                var group = new TAE.EventGroup(type);

                var data = new TAE.EventGroup.EventGroupDataStruct();
                data.DataType = Enum.Parse<TAE.EventGroup.EventGroupDataType>(groupEl.Element("dataType")!.Value);
                data.Area = sbyte.Parse(groupEl.Element("area")!.Value);
                data.Block = sbyte.Parse(groupEl.Element("block")!.Value);
                data.CutsceneEntityType = Enum.Parse<TAE.EventGroup.EventGroupDataStruct.EntityTypes>(groupEl.Element("cutsceneEntityType")!.Value);
                data.CutsceneEntityIDPart1 = short.Parse(groupEl.Element("cutsceneEntityId1")!.Value);
                data.CutsceneEntityIDPart2 = short.Parse(groupEl.Element("cutsceneEntityId2")!.Value);

                group.GroupData = data;

                anim.EventGroups.Add(group);

                var eventsEl = groupEl.Element("events");
                if (eventsEl != null)
                {
                    foreach (XElement evEl in eventsEl.Elements("event"))
                    {
                        var evType = int.Parse(evEl.Element("type")!.Value);
                        var unk04 = int.Parse(evEl.Element("unk04")!.Value);
                        var startTime = float.Parse(evEl.Element("startTime")!.Value);
                        var endTime = float.Parse(evEl.Element("endTime")!.Value);
                        var isUnk = bool.Parse(evEl.Element("isUnk")?.Value ?? "False");
                        var hasTemplate = template.Any(t => t.Value.ContainsKey(evType));
                        if (!hasTemplate)
                            errorService.RegisterNotice($"Missing template for TAE event type {evType}.");
                        if (!isUnk && hasTemplate)
                        {
                            var ev = new TAE.Event(startTime, endTime, evType, unk04, tae.BigEndian, template.First(t => t.Value.ContainsKey(evType)).Value[evType]);
                            ev.Group = group;

                            var paramsEl = evEl.Element("params");
                            if (paramsEl != null)
                            {
                                foreach (XElement paramEl in paramsEl.Elements("param"))
                                {
                                    var key = paramEl.Attribute("name")!.Value;
                                    var value = paramEl.Attribute("value")!.Value;

                                    ev.Parameters[key] = ev.Parameters.Template[key].StringToValue(value);
                                }
                            }
                            anim.Events.Add(ev);
                        }
                        else
                        {
                            var paramBytes = evEl.Element("unkParams")!.Value.Split(",").Select(s => byte.Parse(s)).ToArray();
                            var ev = new TAE.Event(startTime, endTime, evType, unk04, paramBytes, tae.BigEndian);
                            ev.Group = group;
                            anim.Events.Add(ev);
                        }

                    }
                }
            }

            return anim;
    }
}