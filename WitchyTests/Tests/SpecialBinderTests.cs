using SoulsFormats;
using WitchyBND;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyTests;

[TestFixture]
public class SpecialBinderTests : TestBase
{
    [Test]
    public void FFXBNDModern()
    {
        IEnumerable<string> paths = GetSamples("FFXBNDModern");

        var parser = new WFFXBNDModern();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            byte[] backup = { };
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
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            byte[] backup = { };
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
                    string name = Path.Combine(destPath, !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name) : file.Name);
                    Assert.That(File.Exists(name));
                }

                parser.Repack(destPath);
                var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));

                Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
            }
        }
    }

    [Test]
    public void MTDBND()
    {
        IEnumerable<string> paths = GetSamples("MTDBND");

        var parser = new WMTDBND();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            byte[] backup = { };
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
                    string name = Path.Combine(destPath, !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name) : file.Name);
                    Assert.That(File.Exists(name));
                }

                parser.Repack(destPath);
                var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));

                Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
            }
        }
    }
}