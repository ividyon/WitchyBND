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
        IEnumerable<string> paths = GetSamples("FFXBNDModern").Union(GetSamples("FFXBND"));

        var parser = new WFFXBNDModern();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out var file));

            parser.Unpack(path, file, false);
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

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            var root = WBUtil.FindCommonBndRootPath(bnd.Files.Select(a => a.Name));
            foreach (BinderFile file in bnd.Files)
            {
                string name = Path.Combine(destPath,
                    !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name).Replace('\\', Path.DirectorySeparatorChar) : file.Name.Replace('\\', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(name));
            }

            parser.Repack(destPath, false);
            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));

            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
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

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            var root = WBUtil.FindCommonBndRootPath(bnd.Files.Select(a => a.Name));
            foreach (BinderFile file in bnd.Files)
            {
                string name = Path.Combine(destPath,
                    !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, file.Name).Replace('\\', Path.DirectorySeparatorChar) : file.Name.Replace('\\', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(name));
            }

            parser.Repack(destPath, false);
            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));

            Assert.That(File.Exists(parser.GetRepackDestPath(destPath, xml)));
        }
    }

    [Test]
    public void ANIBND4()
    {
        IEnumerable<string> paths = GetSamples("ANIBND4");

        var parser = new WANIBND4();

        foreach (string path in paths.Select(GetCopiedPath))
        {
            SetLocation(path);
            Assert.That(parser.Exists(path));
            Assert.That(parser.Is(path, null, out ISoulsFile? outFile));

            var bnd = BND4.Read(path);

            parser.Unpack(path, outFile, false);
            string? destPath = parser.GetUnpackDestPath(path, false);

            File.Delete(path);

            Assert.That(Directory.Exists(destPath));
            Assert.That(parser.ExistsUnpacked(destPath));
            Assert.That(parser.IsUnpacked(destPath));

            var root = WBUtil.FindCommonBndRootPath(bnd.Files.Select(a => a.Name));
            foreach (BinderFile file in bnd.Files)
            {
                string name = Path.Combine(destPath,
                    WBUtil.UnrootBNDPath(file.Name, root).Replace('\\', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(name));
            }

            parser.Repack(destPath, false);
            var xml = WFileParser.LoadXml(parser.GetFolderXmlPath(destPath));

            var repackDestPath = parser.GetRepackDestPath(destPath, xml);
            Assert.That(File.Exists(repackDestPath));
            var bnd2 = BND4.Read(repackDestPath);
            var mismatches = bnd.Files.Where(f =>
                !WBUtil.MorphemeExtensions.Contains(Path.GetExtension(f.Name)) && bnd2.Files.FirstOrDefault(f2 => f.ID == f2.ID && f.Name == f2.Name) == null).ToList();
            Assert.That(mismatches.Count, Is.Zero,
                $"{Path.GetFileName(path)} has {mismatches.Count} mismatches in ID/Name:\n{string.Join("\n",mismatches.Select(a => {
                    return a.Name; }))}");
        }
    }

    public SpecialBinderTests(bool a, bool b) : base(a, b)
    {
    }
}