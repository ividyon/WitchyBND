using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMSBEFolder
{
    public override bool IsUnpacked(string path)
    {
        if (base.IsUnpacked(path))
        {
            return WarnAboutMSBs();
        }
        return false;
    }

    public override void Repack(string srcPath)
    {
        var msb = new MSBE();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        DCX.Type compression = Enum.Parse<DCX.Type>(xml.Element("compression")?.Value ?? "None");
        msb.Compression = compression;

        var taskList = new List<Task>();

        taskList.Add(new Task(() => {
            foreach (XElement el in xml.Element("events")!.Elements("event"))
            {
                var type = el.Attribute("type")!.Value;
                var fileName = el.Attribute("name")!.Value;
                msb.Events.Add(RepackEvent(srcPath, type, fileName));
            }
        }));
        taskList.Add(new Task(() => {
            foreach (XElement el in xml.Element("parts")!.Elements("part"))
            {
                var type = el.Attribute("type")!.Value;
                var fileName = el.Attribute("name")!.Value;
                msb.Parts.Add(RepackPart(srcPath, type, fileName));
            }
        }));
        taskList.Add(new Task(() => {
            foreach (XElement el in xml.Element("regions")!.Elements("region"))
            {
                var type = el.Attribute("type")!.Value;
                var fileName = el.Attribute("name")!.Value;
                msb.Regions.Add(RepackRegion(srcPath, type, fileName));
            }
        }));
        taskList.Add(new Task(() => {
            foreach (XElement el in xml.Element("models")!.Elements("model"))
            {
                var type = el.Attribute("type")!.Value;
                var fileName = el.Attribute("name")!.Value;
                msb.Models.Add(RepackModel(srcPath, type, fileName));
            }
        }));
        taskList.Add(new Task(() => {
            foreach (XElement el in xml.Element("routes")!.Elements("route"))
            {
                var type = el.Attribute("type")!.Value;
                var fileName = el.Attribute("name")!.Value;
                msb.Routes.Add(RepackRoute(srcPath, type, fileName));
            }
        }));

        if (Configuration.Active.Parallel)
        {
            Parallel.ForEach(taskList, task => {
                task.Start();
                task.Wait();
            });
        }
        else
        {
            taskList.ForEach(task => {
                task.Start();
                task.Wait();
            });
        }

        msb.Layers.Version = Convert.ToInt32(xml.Element("layers")!.Attribute("version")!.Value);

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);
        msb.Write(outPath);
    }

    private MSBE.Event RepackEvent(string srcPath, string type, string fileName)
    {
        var filePath = Path.Combine(srcPath, "Event", type, $"{fileName}.xml");
        switch (type)
        {
            case "Generator":
                return WBUtil.XmlDeserialize<MSBE.Event.Generator>(filePath);
            case "Mount":
                return WBUtil.XmlDeserialize<MSBE.Event.Mount>(filePath);
            case "Navmesh":
                return WBUtil.XmlDeserialize<MSBE.Event.Navmesh>(filePath);
            case "ObjAct":
                return WBUtil.XmlDeserialize<MSBE.Event.ObjAct>(filePath);
            case "Other":
                return WBUtil.XmlDeserialize<MSBE.Event.Other>(filePath);
            case "PatrolInfo":
                return WBUtil.XmlDeserialize<MSBE.Event.PatrolInfo>(filePath);
            case "PlatoonInfo":
                return WBUtil.XmlDeserialize<MSBE.Event.PlatoonInfo>(filePath);
            case "PseudoMultiplayer":
                return WBUtil.XmlDeserialize<MSBE.Event.PseudoMultiplayer>(filePath);
            case "RetryPoint":
                return WBUtil.XmlDeserialize<MSBE.Event.RetryPoint>(filePath);
            case "SignPool":
                return WBUtil.XmlDeserialize<MSBE.Event.SignPool>(filePath);
            case "Treasure":
                return WBUtil.XmlDeserialize<MSBE.Event.Treasure>(filePath);
            case "AreaTeam":
                return WBUtil.XmlDeserialize<MSBE.Event.AreaTeam>(filePath);
            default:
                throw new ArgumentOutOfRangeException(type);
        }
    }

    private MSBE.Region RepackRegion(string srcPath, string type, string fileName)
    {
        var filePath = Path.Combine(srcPath, "Region", type, $"{fileName}.xml");

        switch (type)
        {
            case "AutoDrawGroupPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.AutoDrawGroupPoint>(filePath);
            case "BuddySummonPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.BuddySummonPoint>(filePath);
            case "Connection":
                return WBUtil.XmlDeserialize<MSBE.Region.Connection>(filePath);
            case "DisableTumbleweed":
                return WBUtil.XmlDeserialize<MSBE.Region.DisableTumbleweed>(filePath);
            case "Dummy":
                return WBUtil.XmlDeserialize<MSBE.Region.Dummy>(filePath);
            case "EnvironmentMapEffectBox":
                return WBUtil.XmlDeserialize<MSBE.Region.EnvironmentMapEffectBox>(filePath);
            case "EnvironmentMapOutput":
                return WBUtil.XmlDeserialize<MSBE.Region.EnvironmentMapOutput>(filePath);
            case "EnvironmentMapPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.EnvironmentMapPoint>(filePath);
            case "FallPreventionRemoval":
                return WBUtil.XmlDeserialize<MSBE.Region.FallPreventionRemoval>(filePath);
            case "FastTravelRestriction":
                return WBUtil.XmlDeserialize<MSBE.Region.FastTravelRestriction>(filePath);
            case "GroupDefeatReward":
                return WBUtil.XmlDeserialize<MSBE.Region.GroupDefeatReward>(filePath);
            case "Hitset":
                return WBUtil.XmlDeserialize<MSBE.Region.Hitset>(filePath);
            case "HorseRideOverride":
                return WBUtil.XmlDeserialize<MSBE.Region.HorseRideOverride>(filePath);
            case "LockedMountJump":
                return WBUtil.XmlDeserialize<MSBE.Region.LockedMountJump>(filePath);
            case "LockedMountJumpFall":
                return WBUtil.XmlDeserialize<MSBE.Region.LockedMountJumpFall>(filePath);
            case "InvasionPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.InvasionPoint>(filePath);
            case "MapNameOverride":
                return WBUtil.XmlDeserialize<MSBE.Region.MapNameOverride>(filePath);
            case "MapPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.MapPoint>(filePath);
            case "MapPointDiscoveryOverride":
                return WBUtil.XmlDeserialize<MSBE.Region.MapPointDiscoveryOverride>(filePath);
            case "MapPointParticipationOverride":
                return WBUtil.XmlDeserialize<MSBE.Region.MapPointParticipationOverride>(filePath);
            case "Message":
                return WBUtil.XmlDeserialize<MSBE.Region.Message>(filePath);
            case "MountJump":
                return WBUtil.XmlDeserialize<MSBE.Region.MountJump>(filePath);
            case "MountJumpFall":
                return WBUtil.XmlDeserialize<MSBE.Region.MountJumpFall>(filePath);
            case "MufflingBox":
                return WBUtil.XmlDeserialize<MSBE.Region.MufflingBox>(filePath);
            case "MufflingPlane":
                return WBUtil.XmlDeserialize<MSBE.Region.MufflingPlane>(filePath);
            case "MufflingPortal":
                return WBUtil.XmlDeserialize<MSBE.Region.MufflingPortal>(filePath);
            case "NavmeshCutting":
                return WBUtil.XmlDeserialize<MSBE.Region.NavmeshCutting>(filePath);
            case "Other":
                return WBUtil.XmlDeserialize<MSBE.Region.Other>(filePath);
            case "PatrolRoute":
                return WBUtil.XmlDeserialize<MSBE.Region.PatrolRoute>(filePath);
            case "PatrolRoute22":
                return WBUtil.XmlDeserialize<MSBE.Region.PatrolRoute22>(filePath);
            case "PlayArea":
                return WBUtil.XmlDeserialize<MSBE.Region.PlayArea>(filePath);
            case "SFX":
                return WBUtil.XmlDeserialize<MSBE.Region.SFX>(filePath);
            case "Sound":
                return WBUtil.XmlDeserialize<MSBE.Region.Sound>(filePath);
            case "SoundRegion":
                return WBUtil.XmlDeserialize<MSBE.Region.SoundRegion>(filePath);
            case "SpawnPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.SpawnPoint>(filePath);
            case "WeatherCreateAssetPoint":
                return WBUtil.XmlDeserialize<MSBE.Region.WeatherCreateAssetPoint>(filePath);
            case "WeatherOverride":
                return WBUtil.XmlDeserialize<MSBE.Region.WeatherOverride>(filePath);
            case "WindArea":
                return WBUtil.XmlDeserialize<MSBE.Region.WindArea>(filePath);
            case "WindSFX":
                return WBUtil.XmlDeserialize<MSBE.Region.WindSFX>(filePath);
            default:
                throw new ArgumentOutOfRangeException(type);
        }
    }



    private MSBE.Model RepackModel(string srcPath, string type, string fileName)
    {
        var filePath = Path.Combine(srcPath, "Model", type, $"{fileName}.xml");

        switch (type)
        {
            case "Asset":
                return WBUtil.XmlDeserialize<MSBE.Model.Asset>(filePath);
            case "Collision":
                return WBUtil.XmlDeserialize<MSBE.Model.Collision>(filePath);
            case "Enemy":
                return WBUtil.XmlDeserialize<MSBE.Model.Enemy>(filePath);
            case "MapPiece":
                return WBUtil.XmlDeserialize<MSBE.Model.MapPiece>(filePath);
            case "Player":
                return WBUtil.XmlDeserialize<MSBE.Model.Player>(filePath);
            default:
                throw new ArgumentOutOfRangeException(type);
        }
    }


    private MSBE.Part RepackPart(string srcPath, string type, string fileName)
    {
        var filePath = Path.Combine(srcPath, "Part", type, $"{fileName}.xml");

        switch (type)
        {
            case "Asset":
                return WBUtil.XmlDeserialize<MSBE.Part.Asset>(filePath);
            case "Collision":
                return WBUtil.XmlDeserialize<MSBE.Part.Collision>(filePath);
            case "ConnectCollision":
                return WBUtil.XmlDeserialize<MSBE.Part.ConnectCollision>(filePath);
            case "DummyAsset":
                return WBUtil.XmlDeserialize<MSBE.Part.DummyAsset>(filePath);
            case "DummyEnemy":
                return WBUtil.XmlDeserialize<MSBE.Part.DummyEnemy>(filePath);
            case "Enemy":
                return WBUtil.XmlDeserialize<MSBE.Part.Enemy>(filePath);
            case "EnemyBase":
                return WBUtil.XmlDeserialize<MSBE.Part.EnemyBase>(filePath);
            case "MapPiece":
                return WBUtil.XmlDeserialize<MSBE.Part.MapPiece>(filePath);
            case "Player":
                return WBUtil.XmlDeserialize<MSBE.Part.Player>(filePath);
            default:
                throw new ArgumentOutOfRangeException(type);
        }
    }

    private MSBE.Route RepackRoute(string srcPath, string type, string fileName)
    {
        var filePath = Path.Combine(srcPath, "Route", type, $"{fileName}.xml");

        switch (type)
        {
            case "MufflingBoxLink":
                return WBUtil.XmlDeserialize<MSBE.Route.MufflingBoxLink>(filePath);
            case "MufflingPortalLink":
                return WBUtil.XmlDeserialize<MSBE.Route.MufflingPortalLink>(filePath);
            case "Other":
                return WBUtil.XmlDeserialize<MSBE.Route.Other>(filePath);
            default:
                throw new ArgumentOutOfRangeException(type);
        }
    }
}