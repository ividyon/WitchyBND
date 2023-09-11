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

            Assert.IsTrue(parser.Exists(newPath));
            Assert.IsTrue(parser.Is(newPath));

            parser.Unpack(newPath);
            var destPath = parser.GetUnpackDestPath(newPath);

            Assert.IsTrue(File.Exists(destPath));

            FileInfo fileInfo = new FileInfo(newPath);
            fileInfo.IsReadOnly = false;
            File.Delete(newPath);

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

            Assert.IsTrue(parser.Exists(newPath));
            Assert.IsTrue(parser.Is(newPath));

            parser.Unpack(newPath);
            var destPath = parser.GetUnpackDestDir(newPath);

            Assert.IsTrue(Directory.Exists(destPath));
            File.Delete(newPath);

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
}