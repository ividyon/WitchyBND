using WitchyBND;
using WitchyBND.Parsers;

namespace WitchyTests;

[TestFixture(true, true)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(false, false)]
public class BinderTests : TestBase
{
    public void DCX()
    {
        Configuration.Active.Dcx = true;

        IEnumerable<string> paths = GetSamples("DCX");

        var parser = new WDCX();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            // Unpack

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            var destPath = parser.GetUnpackDestPath(path, false);

            Assert.That(File.Exists(destPath));

            File.Delete(path);

            // Repack

            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            parser.Repack(destPath, false);

            var xml = WFileParser.LoadXml(parser.GetXmlPath(destPath, true));
            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void BND3()
    {
        IEnumerable<string> paths = GetSamples("BND3");
        var parser = new WBND3();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            // Unpack
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            var destPath = parser.GetUnpackDestPath(path, false);

            Assert.That(Directory.Exists(destPath));
            File.Delete(path);

            // Repack
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            parser.Repack(destPath, false);

            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));
            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void BND4()
    {
        IEnumerable<string> paths = GetSamples("BND4");

        var parser = new WBND4();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath, false);

            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));
            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void BXF4()
    {
        IEnumerable<string> paths = GetSamples("BXF4", "*.tpfbhd");

        var parser = new WBXF4();

        foreach (string oldPath in paths)
        {
            var path = oldPath.Replace("Samples", "Results");
            SetLocation(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(oldPath, path);
            File.Copy(oldPath.Replace(".tpfbhd", ".tpfbdt"), path.Replace(".tpfbhd", ".tpfbdt"));


            for (int j = 0; j < 2; j++)
            {
                if (j == 1)
                {
                    Directory.Delete(parser.GetUnpackDestPath(path, false), true);
                    path = path.Replace(".tpfbhd", ".tpfbdt");
                }

                Assert.That(parser.Exists(path));
                Assert.That(parser.Is(path, null, out var outFile));

                parser.Unpack(path, outFile, false);
                string? destPath = parser.GetUnpackDestPath(path, false);

                File.Delete(path);

                Assert.That(Directory.Exists(destPath));
                Assert.That(parser.ExistsUnpacked(destPath));
                Assert.That(parser.IsUnpacked(destPath));
                parser.Repack(destPath, false);

                var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));
                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml, "bhd_filename")));
                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml, "bdt_filename")));
            }
        }
    }

    [Test]
    public void BXF3()
    {
        IEnumerable<string> paths = GetSamples("BXF3", "*.tpfbhd");

        var parser = new WBXF3();

        foreach (string oldPath in paths)
        {
            var path = oldPath.Replace("Samples", "Results");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(oldPath, path);
            File.Copy(oldPath.Replace(".tpfbhd", ".tpfbdt"), path.Replace(".tpfbhd", ".tpfbdt"));


            for (int j = 0; j < 2; j++)
            {
                SetLocation(path);
                if (j == 1)
                {
                    Directory.Delete(parser.GetUnpackDestPath(path, false), true);
                    path = path.Replace(".tpfbhd", ".tpfbdt");
                }

                Assert.That(parser.Exists(path));
                Assert.That(parser.Is(path, null, out var outFile));

                parser.Unpack(path, outFile, false);
                string? destPath = parser.GetUnpackDestPath(path, false);

                File.Delete(path);

                Assert.That(Directory.Exists(destPath));
                Assert.That(parser.ExistsUnpacked(destPath));
                Assert.That(parser.IsUnpacked(destPath));
                parser.Repack(destPath, false);

                var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));
                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml, "bhd_filename")));
                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml, "bdt_filename")));
            }
        }
    }

    [Test]
    public void TPF()
    {
        IEnumerable<string> paths = GetSamples("TPF");

        var parser = new WTPF();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath, false);

            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));
            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    public BinderTests(bool a, bool b) : base(a, b)
    {
    }
}