using System.Xml;
using System.Xml.Linq;
using WitchyBND;
using WitchyBND.CliModes;
using WitchyBND.Parsers;
using WitchyLib;
using static System.Enum;

namespace WitchyTests;

[TestFixture]
public class RegulationTests : TestBase
{

    [Test]
    public void PARAMBND3()
    {
        IEnumerable<string> paths = GetSamples("PARAMBND3");

        var parser = new WPARAMBND3();

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


                var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath, "bnd3"));
                XElement? gameElement = xml.Element("game");
                if (gameElement == null) throw new XmlException("XML has no Game element");
                TryParse(xml.Element("game")!.Value, out WBUtil.GameType game);

                string fullPath = Path.GetDirectoryName(Path.GetFullPath(path))!.TrimEnd(Path.DirectorySeparatorChar);
                string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
                var dirGame = Parse<WBUtil.GameType>(gameName);

                Assert.That(game, Is.EqualTo(dirGame),
                    $"XML game {game.ToString()} was not directory game {dirGame.ToString()}");

                File.Delete(path);

                Assert.That(Directory.Exists(destPath));
                Assert.That(parser.ExistsUnpacked(destPath));
                Assert.That(parser.IsUnpacked(destPath));
                parser.Repack(destPath);

                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)), Is.True);
            }
        }
    }

    [Test]
    // [Category("SkipOnGitHubAction")]
    public void PARAMBND4()
    {
        IEnumerable<string> paths = GetSamples("PARAMBND4");

        var parser = new WPARAMBND4();
        var paramParser = new WPARAM();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path))!.TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
            var dirGame = Parse<WBUtil.GameType>(gameName);

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

                var xml = WFileParser.LoadXml(parser.GetBinderXmlPath(destPath, "bnd4"));
                XElement? gameElement = xml.Element("game");
                if (gameElement == null) throw new XmlException("XML has no Game element");
                TryParse(xml.Element("game")!.Value, out WBUtil.GameType game);

                Assert.That(game, Is.EqualTo(dirGame),
                    $"XML game {game.ToString()} was not directory game {dirGame.ToString()}");

                // foreach (var paramPath in Directory.GetFiles(destPath, "*.param"))
                // {
                //     paramParser.Unpack(paramPath, null, true);
                // }

                File.Delete(path);

                Assert.That(Directory.Exists(destPath));
                Assert.That(parser.ExistsUnpacked(destPath));
                Assert.That(parser.IsUnpacked(destPath));
                parser.Repack(destPath);

                Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)), Is.True);
            }
        }
    }
}