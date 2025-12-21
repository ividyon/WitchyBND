using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using WitchyLib;
using SoulsFormats;
using WitchyFormats;

namespace WitchyBND.Parsers;

public partial class WTAEFile
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

        XElement xml = LoadXml(srcPath);

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

        var animsEl = xml.Element("anims");
        if (animsEl != null && animsEl.Elements("anim").Any())
        {
            var animEls = animsEl.Elements("anim").ToList();
            var bag = new ConcurrentBag<TAE.Animation>();

            void Callback(XElement animEl)
            {
                bag.Add(RepackAnim(animEl, tae, template));
            }

            if (Configuration.Active.Parallel)
            {
                Parallel.ForEach(animEls, Callback);
            }
            else
            {
                animEls.ForEach(Callback);
            }

            tae.Animations = bag.OrderBy(a => a.ID).ToList();
        }

        tae.ApplyTemplate(template);

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);
        tae.Write(outPath);
    }

    private TAE.Animation RepackAnim(XElement animEl, TAE tae, TAE.Template template)
    {
        var animName = animEl.Element("name")!.Value;
        var id = long.Parse(animEl.Element("id")!.Value);
        var headerEl = animEl.Element("header")!;
        var headerType = Enum.Parse<TAE.Animation.MiniHeaderType>(headerEl.Element("type")!.Value);
        var headerIsNull = bool.Parse(headerEl.Element("null")?.Value ?? "False");
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

        header.IsNullHeader = headerIsNull;

        var anim = new TAE.Animation(id, header, animName);
        anim.Events = new();
        anim.EventGroups = new();
        foreach (XElement evEl in animEl.Element("events")!.Elements("event"))
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

                var paramsEl = evEl.Element("params");
                if (paramsEl != null)
                {
                    foreach (XElement paramEl in paramsEl.Elements("param"))
                    {
                        var key = paramEl.Attribute("name")!.Value;
                        var value = paramEl.Attribute("value")!.Value;

                        ev.Parameters[key] = ev.Parameters.Template[key].StorageToValue(value);
                    }
                }
                anim.Events.Add(ev);
            }
            else
            {
                var paramBytes = evEl.Element("unkParams")!.Value.Split(",").Select(s => byte.Parse(s)).ToArray();
                var ev = new TAE.Event(startTime, endTime, evType, unk04, paramBytes, tae.BigEndian);
                anim.Events.Add(ev);
            }
        }
        foreach (XElement groupEl in animEl.Element("eventGroups")!.Elements("eventGroup"))
        {
            var type = int.Parse(groupEl.Element("type")!.Value);
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
                    var idx = int.Parse(evEl.Attribute("index")!.Value);
                    var ev = anim.Events.ElementAt(idx)!;
                    ev.Group = group;
                }
            }
        }

        return anim;
    }
}