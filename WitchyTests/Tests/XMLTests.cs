using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyTests;

[TestFixture]
public class XMLTests : TestBase
{
[Test]
    public void FMG()
    {
        List<string> paths = Directory.GetFiles("./Samples/FMG", "*.fmg", SearchOption.AllDirectories).ToList();

        var parser = new WFMG();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void FXR3()
    {
        List<string> paths = Directory.GetFiles("./Samples/FXR3", "*.fxr", SearchOption.AllDirectories).ToList();

        var parser = new WFXR3();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void GPARAM()
    {
        List<string> paths = Directory.GetFiles("./Samples/GPARAM", "*.gparam*", SearchOption.AllDirectories).ToList();

        var parser = new WGPARAM();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void LUAINFO()
    {
        List<string> paths = Directory.GetFiles("./Samples/LUA", "*.luainfo", SearchOption.AllDirectories).ToList();

        var parser = new WLUAINFO();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void LUAGNL()
    {
        List<string> paths = Directory.GetFiles("./Samples/LUA", "*.luagnl", SearchOption.AllDirectories).ToList();

        var parser = new WLUAGNL();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void MATBIN()
    {
        List<string> paths = Directory.GetFiles("./Samples/MATBIN", "*.matbin", SearchOption.AllDirectories).ToList();

        var parser = new WMATBIN();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void MTD()
    {
        List<string> paths = Directory.GetFiles("./Samples/MTD", "*.mtd", SearchOption.AllDirectories).ToList();

        var parser = new WMTD();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void PARAM()
    {
        List<string> paths = Directory.GetFiles("./Samples/PARAM", "*.param", SearchOption.AllDirectories).ToList();

        var parser = new WPARAM();

        foreach (string path in paths)
        {
            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path)).TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
            parser.Game = Enum.Parse<WBUtil.GameType>(gameName);
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestPath(newPath);

            File.Delete(newPath);

            Assert.That(File.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }
}