using WitchyBND;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyTests;

[TestFixture]
public class BinderTests : TestBase
{
    [Test]
    public void DCX()
    {
        Configuration.Dcx = true;

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