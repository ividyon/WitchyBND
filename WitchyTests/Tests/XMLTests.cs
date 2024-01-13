﻿using Pose;
using WitchyBND;
using WitchyBND.CliModes;
using WitchyBND.Parsers;
using WitchyLib;
using Is = Pose.Is;

namespace WitchyTests;

[TestFixture(true, true)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(false, false)]
public class XMLTests : TestBase
{
    [Test]
    public void FMG()
    {
        IEnumerable<string> paths = GetSamples("FMG");

        var parser = new WFMG();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void FXR3()
    {
        IEnumerable<string> paths = GetSamples("FXR3");

        var parser = new WFXR3();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void GPARAM()
    {
        IEnumerable<string> paths = GetSamples("GPARAM");

        var parser = new WGPARAM();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void MQB()
    {
        IEnumerable<string> paths = GetSamples("MQB");

        var parser = new WMQB();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void LUAINFO()
    {
        IEnumerable<string> paths = GetSamples("LUA", "*.luainfo");

        var parser = new WLUAINFO();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void LUAGNL()
    {
        IEnumerable<string> paths = GetSamples("LUA", "*.luagnl");

        var parser = new WLUAGNL();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void MATBIN()
    {
        IEnumerable<string> paths = GetSamples("MATBIN");

        var parser = new WMATBIN();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void MTD()
    {
        IEnumerable<string> paths = GetSamples("MTD");

        var parser = new WMTD();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void PARAM()
    {
        IEnumerable<string> paths = GetSamples("PARAM");

        var parser = ParseMode.Parsers.OfType<WPARAM>().First();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path)).TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            string destPath = parser.GetUnpackDestPath(path);
            parser.Games[Path.GetDirectoryName(destPath)] = (Enum.Parse<WBUtil.GameType>(gameName), 0);
            parser.Unpack(path, file);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void DBSUB()
    {
        IEnumerable<string> paths = GetSamples("DBSUB");

        var parser = new WDBSUB();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestPath(path);

            File.Delete(path);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            var xml = WFileParser.LoadXml(destPath);
        }
    }

    public XMLTests(bool a, bool b) : base(a, b)
    {
    }
}