using WitchyBND;
using WitchyBND.Parsers;

namespace WitchyTests;

[TestFixture]
public class BinderTests : TestBase
{
    [Test]
    public void DCX()
    {
        Configuration.Dcx = true;

        IEnumerable<string> paths = GetSamples("DCX");

        var parser = new WDCX();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            // Unpack

            Assert.IsTrue(parser.Exists(path));
            Assert.IsTrue(parser.Is(path, null, out var outFile));

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
                parser.Unpack(path, outFile);
                var destPath = parser.GetUnpackDestPath(path);

                Assert.IsTrue(File.Exists(destPath));

                File.Delete(path);

                // Repack

                Assert.IsTrue(parser.ExistsUnpacked(destPath));
                Assert.IsTrue(parser.IsUnpacked(destPath));

                parser.Repack(destPath);

                var xml = WFileParser.LoadXml(parser.GetXmlPath(destPath, true));
                Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
            }
        }
    }

    [Test]
    public void BND3()
    {
        IEnumerable<string> paths = GetSamples("BND3");
        var parser = new WBND3();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            // Unpack
            Assert.IsTrue(parser.Exists(path));
            Assert.IsTrue(parser.Is(path, null, out var outFile));

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

                parser.Unpack(path, outFile);
                var destPath = parser.GetUnpackDestDir(path);

                Assert.IsTrue(Directory.Exists(destPath));
                File.Delete(path);

                // Repack
                Assert.IsTrue(parser.ExistsUnpacked(destPath));
                Assert.IsTrue(parser.IsUnpacked(destPath));

                parser.Repack(destPath);

                var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));
                Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml)));
            }
        }
    }

    [Test]
    public void BND4()
    {
        IEnumerable<string> paths = GetSamples("BND4");

        var parser = new WBND4();

        foreach (string path in paths.Select(GetCopiedPath))
        {

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

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
                parser.Unpack(path, outFile);
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
    }

    [Test]
    public void BXF4()
    {
        IEnumerable<string> paths = GetSamples("BXF4", "*.tpfbhd");

        var parser = new WBXF4();

        foreach (string oldPath in paths)
        {
            var path = oldPath.Replace("Samples", "Results");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(oldPath, path);
            File.Copy(oldPath.Replace(".tpfbhd", ".tpfbdt"), path.Replace(".tpfbhd", ".tpfbdt"));


            for (int j = 0; j < 2; j++)
            {
                if (j == 1)
                {
                    Directory.Delete(parser.GetUnpackDestDir(path), true);
                    path = path.Replace(".tpfbhd", ".tpfbdt");
                }
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

                    parser.Unpack(path, outFile);
                    string? destPath = parser.GetUnpackDestDir(path);

                    File.Delete(path);

                    Assert.That(Directory.Exists(destPath));
                    Assert.That(parser.ExistsUnpacked(destPath));
                    Assert.That(parser.IsUnpacked(destPath));
                    parser.Repack(destPath);

                    var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));
                    Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml, "bhd_filename")));
                    Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml, "bdt_filename")));
                }
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
                if (j == 1)
                {
                    Directory.Delete(parser.GetUnpackDestDir(path), true);
                    path = path.Replace(".tpfbhd", ".tpfbdt");
                }
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

                    parser.Unpack(path, outFile);
                    string? destPath = parser.GetUnpackDestDir(path);

                    File.Delete(path);

                    Assert.That(Directory.Exists(destPath));
                    Assert.That(parser.ExistsUnpacked(destPath));
                    Assert.That(parser.IsUnpacked(destPath));
                    parser.Repack(destPath);

                    var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath));
                    Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml, "bhd_filename")));
                    Assert.IsTrue(File.Exists(parser.GetRepackDestPath(destPath, xml, "bdt_filename")));
                }
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

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

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
                parser.Unpack(path, outFile);
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
    }
}