using SoulsFormats;
using WitchyBND;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyTests;

[TestFixture(true, true)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(false, false)]
public class SpecialBinderTests : TestBase
{
    [Test]
    public void FFXBNDModern()
    {
        IEnumerable<string> paths = GetSamples("FFXBNDModern");

        var parser = new WFFXBNDModern();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file);
            string? destPath = parser.GetUnpackDestDir(path);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath);
            var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }

        IEnumerable<string> wrongPaths = GetSamples("FFXBND");
        foreach (string wrongPath in wrongPaths)
        {
            Assert.That(parser.Is(wrongPath, null, out _), Is.False);
        }
    }

    [Test]
    public void MATBINBND()
    {
        IEnumerable<string> paths = GetSamples("MATBINBND");

        var parser = new WMATBINBND();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            var bnd = BND4.Read(path);

            parser.Unpack(path, outFile);
            string? destPath = parser.GetUnpackDestDir(path);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            var root = WBUtil.FindCommonRootPath(bnd.Files.Select(a => a.Name));
            foreach (BinderFile file in bnd.Files)
            {
                string name = Path.Combine(destPath,
                    !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name) : file.Name);
                Assert.That(File.Exists(name));
            }

            parser.Repack(destPath);
            var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void MTDBND()
    {
        IEnumerable<string> paths = GetSamples("MTDBND");

        var parser = new WMTDBND();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            var bnd = BND4.Read(path);

            parser.Unpack(path, outFile);
            string? destPath = parser.GetUnpackDestDir(path);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            var root = WBUtil.FindCommonRootPath(bnd.Files.Select(a => a.Name));
            foreach (BinderFile file in bnd.Files)
            {
                string name = Path.Combine(destPath,
                    !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name) : file.Name);
                Assert.That(File.Exists(name));
            }

            parser.Repack(destPath);
            var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    public SpecialBinderTests(bool a, bool b) : base(a, b)
    {
    }
}