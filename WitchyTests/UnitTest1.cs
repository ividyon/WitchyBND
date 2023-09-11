using System.Xml;
using System.Xml.Linq;
using WitchyBND;
using WitchyBND.Parsers;

namespace WitchyTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Directory.Delete("./Results", true);
        // Directory.CreateDirectory("./Results");
        Thread.Sleep(1000);
    }

    [Test]
    public void DCX()
    {
        WitchyConfiguration.Dcx = true;

        List<string> paths = Directory.GetFiles("./Samples/DCX", "*.dcx", SearchOption.AllDirectories).ToList();

        var parser = new WDCX();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            // Unpack

            Assert.IsTrue(parser.Exists(newPath));
            Assert.IsTrue(parser.Is(newPath));

            parser.Unpack(newPath);
            var destPath = parser.GetUnpackDestPath(newPath);

            Assert.IsTrue(File.Exists(destPath));

            File.Delete(newPath);

            // Repack

            Assert.IsTrue(parser.ExistsUnpacked(destPath));
            Assert.IsTrue(parser.IsUnpacked(destPath));

            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
        }
    }

    [Test]
    public void BND3()
    {
        List<string> paths = Directory.GetFiles("./Samples/BND3", "*bnd*", SearchOption.AllDirectories).ToList();

        var parser = new WBND3();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            var newDir = Path.GetDirectoryName(newPath);
            Directory.CreateDirectory(newDir);

            File.Copy(path, newPath);

            // Unpack
            Assert.IsTrue(parser.Exists(newPath));
            Assert.IsTrue(parser.Is(newPath));

            parser.Unpack(newPath);
            var destPath = parser.GetUnpackDestDir(newPath);

            Assert.IsTrue(Directory.Exists(destPath));
            File.Delete(newPath);

            // Repack
            Assert.IsTrue(parser.ExistsUnpacked(destPath));
            Assert.IsTrue(parser.IsUnpacked(destPath));

            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, Path.GetFileName(newPath))));

        }
    }

    [Test]
    public void BND4()
    {
        List<string> paths = Directory.GetFiles("./Samples/BND4", "*bnd*", SearchOption.AllDirectories).ToList();

        var parser = new WBND4();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestDir(newPath);

            File.Delete(newPath);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, Path.GetFileName(newPath))));
        }
    }

    [Test]
    public void FFXBND()
    {
        List<string> paths = Directory.GetFiles("./Samples/FFXBND", "*ffxbnd*", SearchOption.AllDirectories).ToList();

        var parser = new WFFXBND();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestDir(newPath);

            File.Delete(newPath);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, Path.GetFileName(newPath))));
        }
    }

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

    // [Test]
    // public void PARAM()
    // {
    //     List<string> paths = Directory.GetFiles("./Samples/PARAM", "*.param", SearchOption.AllDirectories).ToList();
    //
    //     var parser = new WPARAM();
    //
    //     foreach (string path in paths)
    //     {
    //         var newPath = path.Replace(@"/Samples/", @"/Results/");
    //         Directory.CreateDirectory(Path.GetDirectoryName(newPath));
    //         File.Copy(path, newPath);
    //
    //         Assert.That(parser.Exists(newPath));
    //         Assert.That(parser.Is(newPath));
    //
    //         parser.Unpack(newPath);
    //         string? destPath = parser.GetUnpackDestPath(newPath);
    //
    //         File.Delete(newPath);
    //
    //         Assert.That(File.Exists(destPath));
    //         Assert.That(parser.ExistsUnpacked(destPath));
    //         Assert.That(parser.IsUnpacked(destPath));
    //         parser.Repack(destPath);
    //
    //         Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath)));
    //     }
    // }

    [Test]
    public void TPF()
    {
        List<string> paths = Directory.GetFiles("./Samples/TPF", "*.tpf", SearchOption.AllDirectories).ToList();

        var parser = new WTPF();

        foreach (string path in paths)
        {
            var newPath = path.Replace(@"/Samples/", @"/Results/");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath);

            Assert.That(parser.Exists(newPath));
            Assert.That(parser.Is(newPath));

            parser.Unpack(newPath);
            string? destPath = parser.GetUnpackDestDir(newPath);

            File.Delete(newPath);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, Path.GetFileName(newPath))));
        }
    }
}