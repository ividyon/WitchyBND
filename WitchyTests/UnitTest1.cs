using WitchyBND.Parsers;

namespace WitchyTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void DCX()
    {
        List<string> paths = new()
        {
            "./Samples/DCX/ER/systex.tpf.dcx",
            "./Samples/DCX/AC6/eventparam.parambnd.dcx"
        };

        var parser = new WDCX();

        foreach (string path in paths)
        {
            Assert.IsTrue(parser.Exists(path));
            Assert.IsTrue(parser.Is(path));

            parser.Unpack(path);
            var destPath = parser.GetUnpackDestPath(path);

            Assert.IsTrue(File.Exists(destPath));
            var movedPath = $@"./Results/{Path.GetFileName(destPath)}";
            var movedXmlPath = $@"./Results/{Path.GetFileName(parser.GetXmlPath(destPath, true))}";
            Directory.CreateDirectory(Path.GetDirectoryName(movedPath));
            File.Move(destPath, movedPath);
            File.Move(destPath, movedXmlPath);

            Assert.IsTrue(parser.ExistsUnpacked(movedPath));
            Assert.IsTrue(parser.IsUnpacked(movedPath));

            parser.Repack(movedPath);

            Assert.IsTrue(File.Exists(parser.GetRepackDestPath(movedPath)));
        }
    }
}