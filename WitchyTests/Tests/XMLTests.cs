using Pose;
using WitchyBND;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyTests;

[TestFixture]
public class XMLTests : TestBase
{
    [Test]
    public void FMG()
    {
        IEnumerable<string> paths = GetSamples("FMG");

        var parser = new WFMG();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void FXR3()
    {
        IEnumerable<string> paths = GetSamples("FXR3");

        var parser = new WFXR3();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void GPARAM()
    {
        IEnumerable<string> paths = GetSamples("GPARAM");

        var parser = new WGPARAM();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void LUAINFO()
    {
        IEnumerable<string> paths = GetSamples("LUA", "*.luainfo");

        var parser = new WLUAINFO();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void LUAGNL()
    {
        IEnumerable<string> paths = GetSamples("LUA", "*.luagnl");

        var parser = new WLUAGNL();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void MATBIN()
    {
        IEnumerable<string> paths = GetSamples("MATBIN");

        var parser = new WMATBIN();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void MTD()
    {
        IEnumerable<string> paths = GetSamples("MTD");

        var parser = new WMTD();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }
                parser.Unpack(path);
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
    }

    [Test]
    public void PARAM()
    {
        IEnumerable<string> paths = GetSamples("PARAM");

        var parser = new WPARAM();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path)).TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
            parser.Game = Enum.Parse<WBUtil.GameType>(gameName);

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path));

            byte[] backup = {};
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    backup = File.ReadAllBytes(path);
                    Configuration.Args.Location = null;
                }
                else
                {
                    File.WriteAllBytes(path, backup);
                    Configuration.Args.Location = Path.Combine(Path.GetDirectoryName(path), "Target");
                    Directory.CreateDirectory(Configuration.Args.Location);
                }

                Shim exePathShim = Shim.Replace(() => WBUtil.GetExeLocation()).With(() => {
                    return TestContext.CurrentContext.TestDirectory;
                });
                PoseContext.Isolate(() => {
                    parser.Unpack(path);
                }, exePathShim);
                string? destPath = parser.GetUnpackDestPath(path);

                File.Delete(path);

                Assert.That(File.Exists(destPath));
                Assert.That(parser.ExistsUnpacked(destPath));
                Assert.That(parser.IsUnpacked(destPath));

                PoseContext.Isolate(() => {
                    parser.Repack(destPath);
                }, exePathShim);

                var xml = WFileParser.LoadXml(destPath);
                Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
            }
        }
    }
}