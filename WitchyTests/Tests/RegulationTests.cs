using WitchyBND.Parsers;

namespace WitchyTests;

[TestFixture]
public class RegulationTests : TestBase
{

    [Test]
    public void BND3Regulation()
    {
        List<string> paths = Directory.GetFiles("./Samples/BND3Regulation", "*", SearchOption.AllDirectories).ToList();

        var parser = new WBND3Regulation();

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
    public void BND4Regulation()
    {
        List<string> paths = Directory.GetFiles("./Samples/BND4Regulation", "*", SearchOption.AllDirectories).ToList();

        var parser = new WBND4Regulation();

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