using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND;
using WitchyBND.CliModes;
using WitchyBND.Parsers;
using WitchyBND.Services;
using WitchyLib;
using static System.Enum;

namespace WitchyTests;

[TestFixture(true, true)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(false, false)]
public class RegulationTests : TestBase
{

    [Test]
    public void PARAMBND3()
    {
        IEnumerable<string> paths = GetSamples("PARAMBND3");

        var parser = new WPARAMBND3();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath, "bnd3"));

            WBUtil.GameType game = WFileParser.GetGameTypeFromXml(xml);

            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path))!.TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
            var dirGame = Parse<WBUtil.GameType>(gameName);

            Assert.That(game, Is.EqualTo(dirGame),
                $"XML game {game.ToString()} was not directory game {dirGame.ToString()}");

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            parser.Repack(destPath, false);

            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)), Is.True);
        }
    }

    [Test]
    // [Category("SkipOnGitHubAction")]
    public void PARAMBND4()
    {
        IEnumerable<string> paths = GetSamples($"PARAMBND4{Path.DirectorySeparatorChar}Correct");

        var parser = ParseMode.GetParser<WPARAMBND4>();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            string fullPath = Path.GetDirectoryName(Path.GetFullPath(path))!.TrimEnd(Path.DirectorySeparatorChar);
            string gameName = fullPath.Split(Path.DirectorySeparatorChar).Last();
            var dirGame = Parse<WBUtil.GameType>(gameName);

            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var outFile));

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath, "bnd4"));
            WBUtil.GameType game = WFileParser.GetGameTypeFromXml(xml);

            Assert.That(game, Is.EqualTo(dirGame),
                $"XML game {game.ToString()} was not directory game {dirGame.ToString()}");

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            _gameService.DetermineGameType(destPath, IGameService.GameDeterminationType.PARAMBND, dirGame);
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));
            var files = new Dictionary<string, (WFileParser, ISoulsFile)>();
            if (parser.HasPreprocess)
                parser.Preprocess(destPath, false, ref files);
            parser.Repack(destPath, false);

            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)), Is.True);
        }
    }

    [Test]
    // [Category("SkipOnGitHubAction")]
    public void PARAMBND4TooHigh()
    {
        IEnumerable<string> paths = GetSamples($"PARAMBND4{Path.DirectorySeparatorChar}TooHigh");

        var parser = new WPARAMBND4();

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
            Assert.Throws<RegulationOutOfBoundsException>(() => { parser.Repack(destPath, false); });
        }
    }

    [Test]
    // [Category("SkipOnGitHubAction")]
    public void PARAMBND4OutdatedParam()
    {
        IEnumerable<string> paths = GetSamples($"PARAMBND4{Path.DirectorySeparatorChar}OutdatedParam");

        var parser = new WPARAMBND4();

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
            Assert.Throws<MalformedBinderException>(() => parser.Repack(destPath, false));
        }
    }

    public RegulationTests(bool a, bool b) : base(a, b)
    {
    }
}