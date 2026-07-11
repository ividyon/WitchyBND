using System.Buffers.Binary;
using System.Security.Cryptography;
using SoulsFormats;
using SoulsOodleLib;
using WitchyBND;
using WitchyBND.Services;
using WitchyTests.Infrastructure;

namespace WitchyTests;

[TestFixture]
[Category("LocalGameFixture")]
public class LocalOodleFixtureTests
{
    private static readonly object[] Fixtures =
    [
        new object[] { "chr/c0000.anibnd.dcx", 6_872_203, 67_960_282, "1e9ccff8d91ae07f57faa8a94a2f3a9cc10a5f38bb0d8b1db2c05facd82424aa" },
        new object[] { "chr/c0000_a00_hi.anibnd.dcx", 12_190_426, 21_557_252, "e65ae56a4f08190bb8e3679526df470178cc8a8e4fdf892a9737dbd8e4b721c7" },
        new object[] { "chr/c0000_a00_md.anibnd.dcx", 1_038_460, 1_715_364, "d8215940de5b1fd55914fa28d076cfa8dac7474098d3859f25e42dd73f6d7d01" },
        new object[] { "chr/c0000_a00_lo.anibnd.dcx", 171_339, 307_636, "64e9891c0eb350668c2efa2cf0a2def68919586e412598c8d85f94b30d92c0da" },
        new object[] { "chr/c0000_a0x.anibnd.dcx", 2_543_147, 5_028_276, "184fe08ea4a18a6aa5edc17a4747c5428e651ff870b48c030f20cdf39b58d866" },
        new object[] { "chr/c0000_a6x.anibnd.dcx", 57_796, 130_036, "9d35b674bbf1922677de2fab18dd3117048bcdf5b285caee6ac96d61230f1172" },
    ];

    [TestCaseSource(nameof(Fixtures))]
    public void OfficialWineBackendDecodesRecordedKrakenMatrix(
        string relativePath,
        int expectedCompressedSize,
        int expectedRawSize,
        string expectedSha256)
    {
        string? root = FixtureLocator.TryGetRoot();
        if (root == null)
            Assert.Ignore("Set WITCHY_FIXTURES_ROOT to run copyrighted local game fixture tests.");

        string fixture = FixtureLocator.RequireFile(root!, relativePath);
        byte[] dcx = File.ReadAllBytes(fixture);
        Assert.That(dcx.AsSpan(0, 4).SequenceEqual("DCX\0"u8), Is.True);
        int rawSize = BinaryPrimitives.ReadInt32BigEndian(dcx.AsSpan(28, 4));
        int compressedSize = BinaryPrimitives.ReadInt32BigEndian(dcx.AsSpan(32, 4));
        Assert.Multiple(() =>
        {
            Assert.That(compressedSize, Is.EqualTo(expectedCompressedSize));
            Assert.That(rawSize, Is.EqualTo(expectedRawSize));
        });

        var options = new CliOptions
        {
            WineExecutable = Environment.GetEnvironmentVariable("WITCHY_WINE"),
            OodleLibrary = Environment.GetEnvironmentVariable("WITCHY_OODLE_LIBRARY"),
            OodleHelper = Environment.GetEnvironmentVariable("WITCHY_OODLE_HELPER")
        };
        if (string.IsNullOrWhiteSpace(options.WineExecutable) ||
            string.IsNullOrWhiteSpace(options.OodleLibrary) ||
            string.IsNullOrWhiteSpace(options.OodleHelper))
        {
            Assert.Ignore(
                "Set WITCHY_WINE, WITCHY_OODLE_LIBRARY, and WITCHY_OODLE_HELPER to run official decode checks.");
        }

        OodleBackendConfigurator.ConfigureMacOS(options);
        try
        {
            byte[] compressed = dcx.AsSpan(76, compressedSize).ToArray();
            byte[] raw = SoulsFormats.Oodle.GetOodleCompressor().Decompress(compressed, rawSize);
            Assert.That(Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant(), Is.EqualTo(expectedSha256));
        }
        finally
        {
            OodleBackendRegistry.Current = null;
        }
    }
}
