using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WTAEFile
{

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return !Configuration.Active.TaeFolder && IsRead<TAE>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        TAE tae = (file as TAE)!;

        var game = gameService.DetermineGameType(srcPath, IGameService.GameDeterminationType.Other).Item1;
        if (!templateDict.ContainsKey(game))
        {
            throw new GameUnsupportedException(game);
        }

        var template = templateDict[game];
        tae.ApplyTemplate(template);

        XDocument xDoc = new XDocument();
        XElement root = new XElement(XmlTag);
        xDoc.Add(root);
        root.AddE("id", tae.ID);
        root.AddE("compression", tae.Compression);
        root.AddE("game", game);
        root.AddE("format", tae.Format);
        root.AddE("eventBank", tae.EventBank);
        root.AddE("sibName", tae.SibName);
        root.AddE("skeletonName", tae.SkeletonName);
        root.AddE("flags", string.Join(",", tae.Flags));
        root.AddE("bigendian", tae.BigEndian);

        if (Version > 0) root.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (tae.Animations.Any())
        {
            var bag = new ConcurrentBag<XElement>();
            var animsEl = new XElement("anims");
            root.Add(animsEl);
            void Callback(TAE.Animation anim)
            {
                bag.Add(UnpackAnim(tae, anim));
            }

            if (Configuration.Active.Parallel)
            {
                Parallel.ForEach(tae.Animations, Callback);
            }
            else
            {
                tae.Animations.ForEach(Callback);
            }

            foreach (var animEl in bag.OrderBy(a => int.Parse(a.Element("id").Value)).ToList())
            {
                animsEl.Add(animEl);
            }
        }


        var destPath = GetUnpackDestPath(srcPath, recursive);
        AddLocationToXml(srcPath, root);
        xDoc.Save(destPath);
    }

    public XElement UnpackAnim(TAE tae, TAE.Animation anim)
    {
        XElement animEl = new XElement("anim");
        animEl.AddE("id", anim.ID);
        animEl.AddE("name", anim.AnimFileName);
        var header = new XElement("header");
        animEl.Add(header);
        header.AddE("type", anim.MiniHeader.Type);
        if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standard)
        {
            header.AddE("allowDelayLoad", standard.AllowDelayLoad);
            header.AddE("importsHkx", standard.ImportsHKX);
            header.AddE("loopByDefault", standard.IsLoopByDefault);
            header.AddE("importHkxSourceAnimId", standard.ImportHKXSourceAnimID);
        }
        else if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim import)
        {
            header.AddE("animId", import.ImportFromAnimID);
        }

        animEl.AddE("animGroups", anim.EventGroups.Select(group => {
            var groupEl = new XElement("animGroup");
            groupEl.AddE("type", group.GroupType);
            groupEl.AddE("dataType", group.GroupData.DataType);
            groupEl.AddE("area", group.GroupData.Area);
            groupEl.AddE("block", group.GroupData.Block);
            groupEl.AddE("cutsceneEntityType", group.GroupData.CutsceneEntityType);
            groupEl.AddE("cutsceneEntityId1", group.GroupData.CutsceneEntityIDPart1);
            groupEl.AddE("cutsceneEntityId2", group.GroupData.CutsceneEntityIDPart2);
            var events = anim.Events.Where(a => a.Group == group).ToList();
            if (events.Any())
            {
                groupEl.AddE("events", events.Select(ev => {
                    var eventEl = new XElement("event");
                    eventEl.AddE("type", ev.Type);
                    eventEl.AddE("unk04", ev.Unk04);
                    eventEl.AddE("startTime", ev.StartTime);
                    eventEl.AddE("endTime", ev.EndTime);
                    if (tae.AppliedTemplate[tae.EventBank].ContainsKey(ev.Type))
                    {
                        eventEl.AddE("params", ev.Parameters?.Values.Select(p => {
                            var paramEl = new XElement("param");
                            paramEl.SetAttributeValue("name", p.Key);
                            paramEl.SetAttributeValue("value", p.Value);
                            return paramEl;
                        }));
                    }
                    else
                    {
                        eventEl.AddE("isUnk", true);
                        eventEl.AddE("unkParams", string.Join(",", ev.GetParameterBytes(tae.BigEndian)));
                    }

                    return eventEl;
                }));
            }

            return groupEl;
        }));

        return animEl;
    }
}