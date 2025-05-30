using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMSBEFolder
{
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return IsRead<MSBE>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        MSBE msb = (file as MSBE)!;

        string destDir = GetUnpackDestPath(srcPath, recursive);

        var xml = PrepareXmlManifest(srcPath, recursive, true, msb.Compression, out XDocument xDoc, null);

        var taskList = new List<Task>();

        if (Directory.Exists(Path.Combine(destDir, "Event")))
            Directory.Delete(Path.Combine(destDir, "Event"), true);
        Directory.CreateDirectory(Path.Combine(destDir, "Event"));
        if (Directory.Exists(Path.Combine(destDir, "Region")))
            Directory.Delete(Path.Combine(destDir, "Region"), true);
        Directory.CreateDirectory(Path.Combine(destDir, "Region"));
        if (Directory.Exists(Path.Combine(destDir, "Part")))
            Directory.Delete(Path.Combine(destDir, "Part"), true);
        Directory.CreateDirectory(Path.Combine(destDir, "Part"));
        if (Directory.Exists(Path.Combine(destDir, "Model")))
            Directory.Delete(Path.Combine(destDir, "Model"), true);
        Directory.CreateDirectory(Path.Combine(destDir, "Model"));
        if (Directory.Exists(Path.Combine(destDir, "Route")))
            Directory.Delete(Path.Combine(destDir, "Route"), true);
        Directory.CreateDirectory(Path.Combine(destDir, "Route"));

        var eventsEl = new XElement("events");
        xml.Add(eventsEl);
        var regionsEl = new XElement("regions");
        xml.Add(regionsEl);
        var partsEl = new XElement("parts");
        xml.Add(partsEl);
        var modelsEl = new XElement("models");
        xml.Add(modelsEl);
        var routesEl = new XElement("routes");
        xml.Add(routesEl);
        var layersEl = new XElement("layers");
        layersEl.SetAttributeValue("version", msb.Layers.Version);
        xml.Add(layersEl);

        taskList.Add(new Task(() => {
            UnpackEntries(eventsEl, msb.Events.GetEntries().Cast<MSBE.Entry>().ToList(), destDir);
        }));
        taskList.Add(new Task(() => {
          UnpackEntries(regionsEl, msb.Regions.GetEntries().Cast<MSBE.Entry>().ToList(), destDir);
        }));
        taskList.Add(new Task(() => {
            UnpackEntries(partsEl, msb.Parts.GetEntries().Cast<MSBE.Entry>().ToList(), destDir);
        }));
        taskList.Add(new Task(() => {
            UnpackEntries(modelsEl, msb.Models.GetEntries().Cast<MSBE.Entry>().ToList(), destDir);
        }));
        taskList.Add(new Task(() => {
            UnpackEntries(routesEl, msb.Routes.GetEntries().Cast<MSBE.Entry>().ToList(), destDir);
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
        
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    private void UnpackEntries(XElement parentEl, List<MSBE.Entry> entries, string rootDir)
    {
        foreach (MSBE.Entry entry in entries)
        {
            string entryDirName = entry switch
            {
                MSBE.Event => "Event",
                MSBE.Part => "Part",
                MSBE.Region => "Region",
                MSBE.Model => "Model",
                MSBE.Route => "Route",
                _ => throw new ArgumentOutOfRangeException(nameof(entry))
            };

            string entryDir = Path.Combine(rootDir, entryDirName);

            var el = new XElement(entryDirName.ToLower());
            parentEl.Add(el);

            var index = entries.IndexOf(entry);
            var potentialName = WBUtil.GetValidFileName(entry.Name.Trim());
            var fileName = !string.IsNullOrWhiteSpace(potentialName) ? potentialName : $"Event {index:00000}";

            string filePath;

            switch (entry)
            {
                case MSBE.Event.Generator generator:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, generator.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.Generator>(generator, filePath);
                    el.SetAttributeValue("type", generator.GetType().Name);
                    break;
                case MSBE.Event.Mount mount:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mount.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.Mount>(mount, filePath);
                    el.SetAttributeValue("type", mount.GetType().Name);
                    break;
                case MSBE.Event.Navmesh navmesh:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, navmesh.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.Navmesh>(navmesh, filePath);
                    el.SetAttributeValue("type", navmesh.GetType().Name);
                    break;
                case MSBE.Event.ObjAct objAct:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, objAct.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.ObjAct>(objAct, filePath);
                    el.SetAttributeValue("type", objAct.GetType().Name);
                    break;
                case MSBE.Event.Other other:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, other.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.Other>(other, filePath);
                    el.SetAttributeValue("type", other.GetType().Name);
                    break;
                case MSBE.Event.PatrolInfo patrolInfo:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, patrolInfo.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.PatrolInfo>(patrolInfo, filePath);
                    el.SetAttributeValue("type", patrolInfo.GetType().Name);
                    break;
                case MSBE.Event.PlatoonInfo platoonInfo:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, platoonInfo.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.PlatoonInfo>(platoonInfo, filePath);
                    el.SetAttributeValue("type", platoonInfo.GetType().Name);
                    break;
                case MSBE.Event.PseudoMultiplayer pseudoMultiplayer:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, pseudoMultiplayer.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.PseudoMultiplayer>(pseudoMultiplayer, filePath);
                    el.SetAttributeValue("type", pseudoMultiplayer.GetType().Name);
                    break;
                case MSBE.Event.RetryPoint retryPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, retryPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.RetryPoint>(retryPoint, filePath);
                    el.SetAttributeValue("type", retryPoint.GetType().Name);
                    break;
                case MSBE.Event.SignPool signPool:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, signPool.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.SignPool>(signPool, filePath);
                    el.SetAttributeValue("type", signPool.GetType().Name);
                    break;
                case MSBE.Event.Treasure treasure:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, treasure.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.Treasure>(treasure, filePath);
                    el.SetAttributeValue("type", treasure.GetType().Name);
                    break;
                case MSBE.Event.AreaTeam areaTeam:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, areaTeam.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event.AreaTeam>(areaTeam, filePath);
                    el.SetAttributeValue("type", areaTeam.GetType().Name);
                    break;
                case MSBE.Model.Asset asset:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, asset.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model.Asset>(asset, filePath);
                    el.SetAttributeValue("type", asset.GetType().Name);
                    break;
                case MSBE.Model.Collision collision:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, collision.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model.Collision>(collision, filePath);
                    el.SetAttributeValue("type", collision.GetType().Name);
                    break;
                case MSBE.Model.Enemy enemy:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, enemy.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model.Enemy>(enemy, filePath);
                    el.SetAttributeValue("type", enemy.GetType().Name);
                    break;
                case MSBE.Model.MapPiece mapPiece:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapPiece.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model.MapPiece>(mapPiece, filePath);
                    el.SetAttributeValue("type", mapPiece.GetType().Name);
                    break;
                case MSBE.Model.Player player:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, player.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model.Player>(player, filePath);
                    el.SetAttributeValue("type", player.GetType().Name);
                    break;
                case MSBE.Part.Asset asset1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, asset1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.Asset>(asset1, filePath);
                    el.SetAttributeValue("type", asset1.GetType().Name);
                    break;
                case MSBE.Part.Collision collision1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, collision1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.Collision>(collision1, filePath);
                    el.SetAttributeValue("type", collision1.GetType().Name);
                    break;
                case MSBE.Part.ConnectCollision connectCollision:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, connectCollision.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.ConnectCollision>(connectCollision, filePath);
                    el.SetAttributeValue("type", connectCollision.GetType().Name);
                    break;
                case MSBE.Part.DummyAsset dummyAsset:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, dummyAsset.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.DummyAsset>(dummyAsset, filePath);
                    el.SetAttributeValue("type", dummyAsset.GetType().Name);
                    break;
                case MSBE.Part.DummyEnemy dummyEnemy:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, dummyEnemy.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.DummyEnemy>(dummyEnemy, filePath);
                    el.SetAttributeValue("type", dummyEnemy.GetType().Name);
                    break;
                case MSBE.Part.Enemy enemy1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, enemy1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.Enemy>(enemy1, filePath);
                    el.SetAttributeValue("type", enemy1.GetType().Name);
                    break;
                case MSBE.Part.MapPiece mapPiece1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapPiece1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.MapPiece>(mapPiece1, filePath);
                    el.SetAttributeValue("type", mapPiece1.GetType().Name);
                    break;
                case MSBE.Part.Player player1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, player1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.Player>(player1, filePath);
                    el.SetAttributeValue("type", player1.GetType().Name);
                    break;
                case MSBE.Region.AutoDrawGroupPoint autoDrawGroupPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, autoDrawGroupPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.AutoDrawGroupPoint>(autoDrawGroupPoint, filePath);
                    el.SetAttributeValue("type", autoDrawGroupPoint.GetType().Name);
                    break;
                case MSBE.Region.BuddySummonPoint buddySummonPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, buddySummonPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.BuddySummonPoint>(buddySummonPoint, filePath);
                    el.SetAttributeValue("type", buddySummonPoint.GetType().Name);
                    break;
                case MSBE.Region.Connection connection:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, connection.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Connection>(connection, filePath);
                    el.SetAttributeValue("type", connection.GetType().Name);
                    break;
                case MSBE.Region.DisableTumbleweed disableTumbleweed:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, disableTumbleweed.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.DisableTumbleweed>(disableTumbleweed, filePath);
                    el.SetAttributeValue("type", disableTumbleweed.GetType().Name);
                    break;
                case MSBE.Region.Dummy dummy:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, dummy.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Dummy>(dummy, filePath);
                    el.SetAttributeValue("type", dummy.GetType().Name);
                    break;
                case MSBE.Region.EnvironmentMapEffectBox environmentMapEffectBox:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, environmentMapEffectBox.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.EnvironmentMapEffectBox>(environmentMapEffectBox, filePath);
                    el.SetAttributeValue("type", environmentMapEffectBox.GetType().Name);
                    break;
                case MSBE.Region.EnvironmentMapOutput environmentMapOutput:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, environmentMapOutput.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.EnvironmentMapOutput>(environmentMapOutput, filePath);
                    el.SetAttributeValue("type", environmentMapOutput.GetType().Name);
                    break;
                case MSBE.Region.EnvironmentMapPoint environmentMapPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, environmentMapPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.EnvironmentMapPoint>(environmentMapPoint, filePath);
                    el.SetAttributeValue("type", environmentMapPoint.GetType().Name);
                    break;
                case MSBE.Region.FallPreventionRemoval fallPreventionRemoval:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, fallPreventionRemoval.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.FallPreventionRemoval>(fallPreventionRemoval, filePath);
                    el.SetAttributeValue("type", fallPreventionRemoval.GetType().Name);
                    break;
                case MSBE.Region.FastTravelRestriction fastTravelRestriction:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, fastTravelRestriction.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.FastTravelRestriction>(fastTravelRestriction, filePath);
                    el.SetAttributeValue("type", fastTravelRestriction.GetType().Name);
                    break;
                case MSBE.Region.GroupDefeatReward groupDefeatReward:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, groupDefeatReward.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.GroupDefeatReward>(groupDefeatReward, filePath);
                    el.SetAttributeValue("type", groupDefeatReward.GetType().Name);
                    break;
                case MSBE.Region.Hitset hitset:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, hitset.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Hitset>(hitset, filePath);
                    el.SetAttributeValue("type", hitset.GetType().Name);
                    break;
                case MSBE.Region.HorseRideOverride horseRideOverride:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, horseRideOverride.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.HorseRideOverride>(horseRideOverride, filePath);
                    el.SetAttributeValue("type", horseRideOverride.GetType().Name);
                    break;
                case MSBE.Region.LockedMountJump lockedMountJump:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, lockedMountJump.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.LockedMountJump>(lockedMountJump, filePath);
                    el.SetAttributeValue("type", lockedMountJump.GetType().Name);
                    break;
                case MSBE.Region.LockedMountJumpFall lockedMountJumpFall:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, lockedMountJumpFall.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.LockedMountJumpFall>(lockedMountJumpFall, filePath);
                    el.SetAttributeValue("type", lockedMountJumpFall.GetType().Name);
                    break;
                case MSBE.Region.InvasionPoint invasionPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, invasionPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.InvasionPoint>(invasionPoint, filePath);
                    el.SetAttributeValue("type", invasionPoint.GetType().Name);
                    break;
                case MSBE.Region.MapNameOverride mapNameOverride:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapNameOverride.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MapNameOverride>(mapNameOverride, filePath);
                    el.SetAttributeValue("type", mapNameOverride.GetType().Name);
                    break;
                case MSBE.Region.MapPoint mapPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MapPoint>(mapPoint, filePath);
                    el.SetAttributeValue("type", mapPoint.GetType().Name);
                    break;
                case MSBE.Region.MapPointDiscoveryOverride mapPointDiscoveryOverride:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapPointDiscoveryOverride.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MapPointDiscoveryOverride>(mapPointDiscoveryOverride, filePath);
                    el.SetAttributeValue("type", mapPointDiscoveryOverride.GetType().Name);
                    break;
                case MSBE.Region.MapPointParticipationOverride mapPointParticipationOverride:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mapPointParticipationOverride.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MapPointParticipationOverride>(mapPointParticipationOverride,
                        filePath);
                    el.SetAttributeValue("type", mapPointParticipationOverride.GetType().Name);
                    break;
                case MSBE.Region.Message message:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, message.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Message>(message, filePath);
                    el.SetAttributeValue("type", message.GetType().Name);
                    break;
                case MSBE.Region.MountJump mountJump:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mountJump.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MountJump>(mountJump, filePath);
                    el.SetAttributeValue("type", mountJump.GetType().Name);
                    break;
                case MSBE.Region.MountJumpFall mountJumpFall:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mountJumpFall.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MountJumpFall>(mountJumpFall, filePath);
                    el.SetAttributeValue("type", mountJumpFall.GetType().Name);
                    break;
                case MSBE.Region.MufflingBox mufflingBox:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mufflingBox.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MufflingBox>(mufflingBox, filePath);
                    el.SetAttributeValue("type", mufflingBox.GetType().Name);
                    break;
                case MSBE.Region.MufflingPlane mufflingPlane:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mufflingPlane.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MufflingPlane>(mufflingPlane, filePath);
                    el.SetAttributeValue("type", mufflingPlane.GetType().Name);
                    break;
                case MSBE.Region.MufflingPortal mufflingPortal:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mufflingPortal.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.MufflingPortal>(mufflingPortal, filePath);
                    el.SetAttributeValue("type", mufflingPortal.GetType().Name);
                    break;
                case MSBE.Region.NavmeshCutting navmeshCutting:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, navmeshCutting.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.NavmeshCutting>(navmeshCutting, filePath);
                    el.SetAttributeValue("type", navmeshCutting.GetType().Name);
                    break;
                case MSBE.Region.Other other1:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, other1.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Other>(other1, filePath);
                    el.SetAttributeValue("type", other1.GetType().Name);
                    break;
                case MSBE.Region.PatrolRoute patrolRoute:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, patrolRoute.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.PatrolRoute>(patrolRoute, filePath);
                    el.SetAttributeValue("type", patrolRoute.GetType().Name);
                    break;
                case MSBE.Region.PatrolRoute22 patrolRoute22:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, patrolRoute22.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.PatrolRoute22>(patrolRoute22, filePath);
                    el.SetAttributeValue("type", patrolRoute22.GetType().Name);
                    break;
                case MSBE.Region.PlayArea playArea:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, playArea.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.PlayArea>(playArea, filePath);
                    el.SetAttributeValue("type", playArea.GetType().Name);
                    break;
                case MSBE.Region.SFX sfx:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, sfx.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.SFX>(sfx, filePath);
                    el.SetAttributeValue("type", sfx.GetType().Name);
                    break;
                case MSBE.Region.Sound sound:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, sound.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.Sound>(sound, filePath);
                    el.SetAttributeValue("type", sound.GetType().Name);
                    break;
                case MSBE.Region.SoundRegion soundRegion:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, soundRegion.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.SoundRegion>(soundRegion, filePath);
                    el.SetAttributeValue("type", soundRegion.GetType().Name);
                    break;
                case MSBE.Region.SpawnPoint spawnPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, spawnPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.SpawnPoint>(spawnPoint, filePath);
                    el.SetAttributeValue("type", spawnPoint.GetType().Name);
                    break;
                case MSBE.Region.WeatherCreateAssetPoint weatherCreateAssetPoint:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, weatherCreateAssetPoint.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.WeatherCreateAssetPoint>(weatherCreateAssetPoint, filePath);
                    el.SetAttributeValue("type", weatherCreateAssetPoint.GetType().Name);
                    break;
                case MSBE.Region.WeatherOverride weatherOverride:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, weatherOverride.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.WeatherOverride>(weatherOverride, filePath);
                    el.SetAttributeValue("type", weatherOverride.GetType().Name);
                    break;
                case MSBE.Region.WindArea windArea:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, windArea.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.WindArea>(windArea, filePath);
                    el.SetAttributeValue("type", windArea.GetType().Name);
                    break;
                case MSBE.Region.WindSFX windSfx:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, windSfx.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region.WindSFX>(windSfx, filePath);
                    el.SetAttributeValue("type", windSfx.GetType().Name);
                    break;
                case MSBE.Route.MufflingBoxLink mufflingBoxLink:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mufflingBoxLink.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Route.MufflingBoxLink>(mufflingBoxLink, filePath);
                    el.SetAttributeValue("type", mufflingBoxLink.GetType().Name);
                    break;
                case MSBE.Route.MufflingPortalLink mufflingPortalLink:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, mufflingPortalLink.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Route.MufflingPortalLink>(mufflingPortalLink, filePath);
                    el.SetAttributeValue("type", mufflingPortalLink.GetType().Name);
                    break;
                case MSBE.Route.Other other2:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, other2.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Route.Other>(other2, filePath);
                    el.SetAttributeValue("type", other2.GetType().Name);
                    break;
                case MSBE.Event @event:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, @event.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Event>(@event, filePath);
                    el.SetAttributeValue("type", @event.GetType().Name);
                    break;
                case MSBE.Model model:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, model.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Model>(model, filePath);
                    el.SetAttributeValue("type", model.GetType().Name);
                    break;
                case MSBE.Part.EnemyBase enemyBase:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, enemyBase.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part.EnemyBase>(enemyBase, filePath);
                    el.SetAttributeValue("type", enemyBase.GetType().Name);
                    break;
                case MSBE.Region region:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, region.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Region>(region, filePath);
                    el.SetAttributeValue("type", region.GetType().Name);
                    break;
                case MSBE.Route route:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, route.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Route>(route, filePath);
                    el.SetAttributeValue("type", route.GetType().Name);
                    break;
                case MSBE.Part part:
                    filePath = WBUtil.NextAvailableFilename(Path.Combine(entryDir, part.GetType().Name, $"{fileName}.xml"));
                    WBUtil.XmlSerialize<MSBE.Part>(part, filePath);
                    el.SetAttributeValue("type", part.GetType().Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry));
            }

            fileName = Path.GetFileNameWithoutExtension(filePath);
            el.SetAttributeValue("name", fileName);
        }
    }
}