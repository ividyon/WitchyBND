namespace WitchyTests;

[TestFixture(true, true)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(false, false)]
public class ParseModeTest : TestBase
{
    [Test]
    public void ParseMode()
    {
        IEnumerable<string> paths = GetSamples("ParseMode").Select(GetCopiedPath);
        WitchyBND.CliModes.ParseMode.ParseFiles(paths);
    }

    public ParseModeTest(bool location, bool parallel) : base(location, parallel)
    {
    }
}