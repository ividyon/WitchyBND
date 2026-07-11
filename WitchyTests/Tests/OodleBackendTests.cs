using SoulsFormats;
using SoulsOodleLib;
using WitchyBND;
using WitchyBND.Services;

namespace WitchyTests;

[TestFixture]
public class OodleBackendTests
{
    [TearDown]
    public void ResetBackend()
    {
        OodleBackendRegistry.Current = null;
    }

    [Test]
    public void RegistryProvidesConfiguredBackendToSoulsFormats()
    {
        OodleBackendRegistry.Current = new FakeBackend([1, 2, 3]);

        byte[] result = SoulsFormats.Oodle.GetOodleCompressor().Decompress([9], 3);

        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void RegistryRejectsWrongDecompressedSize()
    {
        OodleBackendRegistry.Current = new FakeBackend([1]);

        Assert.Throws<InvalidDataException>(() =>
            SoulsFormats.Oodle.GetOodleCompressor().Decompress([9], 3));
    }

    [Test]
    public void RegistryRejectsUnavailableBackend()
    {
        OodleBackendRegistry.Current = new FakeBackend([1]) { IsAvailable = false };

        Assert.Throws<InvalidOperationException>(() => SoulsFormats.Oodle.GetOodleCompressor());
    }

    [Test]
    public void RegistryRestoresDefaultSelectionWhenCleared()
    {
        OodleBackendRegistry.Current = new FakeBackend([1]);
        OodleBackendRegistry.Current = null;

        Assert.That(SoulsFormats.Oodle.CustomCompressorFactory, Is.Null);
    }

    [Test]
    public void WineDiscoveryPrefersExplicitExecutableIncludingSpaces()
    {
        string expected = Path.GetFullPath("/Applications/Wine Test/bin/wine64");

        string? actual = WineDiscovery.Find(
            expected,
            path => path == expected,
            _ => null);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void WineDiscoveryFallsBackToPath()
    {
        string expected = Path.GetFullPath("/custom/bin/wine64");

        string? actual = WineDiscovery.Find(
            null,
            path => path == expected,
            name => name == "PATH" ? "/custom/bin" : null);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("oo2core_6_win64.dll", OodleVersion.Oodle6)]
    [TestCase("oo2core_8_win64.dll", OodleVersion.Oodle8)]
    [TestCase("oo2core_9_win64.dll", OodleVersion.Oodle9)]
    public void ConfiguratorDeterminesOodleVersionFromLibraryName(string name, OodleVersion expected)
    {
        Assert.That(OodleBackendConfigurator.ParseVersion(name), Is.EqualTo(expected));
    }

    [Test]
    public void ConfiguratorRejectsAmbiguousLibraryName()
    {
        Assert.Throws<InvalidDataException>(() =>
            OodleBackendConfigurator.ParseVersion("oo2core_win64.dll"));
    }

    [Test]
    public void ConfiguratorUsesPersistedOodleSettings()
    {
        var active = new Configuration.ActiveConfig
        {
            OodleLibrary = "/stored/oo2core_6_win64.dll",
            WineExecutable = "/stored/wine",
            OodleHelper = "/stored/helper.exe"
        };

        OodleBackendSettings settings = OodleBackendConfigurator.ResolveSettings(
            new CliOptions(), active, _ => null);

        Assert.That(settings.Library, Is.EqualTo(active.OodleLibrary));
        Assert.That(settings.Wine, Is.EqualTo(active.WineExecutable));
        Assert.That(settings.Helper, Is.EqualTo(active.OodleHelper));
    }

    [Test]
    public void ActivatingStoredConfigurationCopiesOodleSettings()
    {
        var previous = Configuration.Active;
        Configuration.Active = new Configuration.ActiveConfig();
        var stored = new Configuration.StoredConfig
        {
            OodleLibrary = "/stored/oo2core_6_win64.dll",
            WineExecutable = "/stored/wine",
            OodleHelper = "/stored/helper.exe"
        };

        try
        {
            Configuration.ActivateStoredConfiguration(stored);

            Assert.That(Configuration.Active.OodleLibrary, Is.EqualTo(stored.OodleLibrary));
            Assert.That(Configuration.Active.WineExecutable, Is.EqualTo(stored.WineExecutable));
            Assert.That(Configuration.Active.OodleHelper, Is.EqualTo(stored.OodleHelper));
        }
        finally
        {
            Configuration.Active = previous;
        }
    }

    [Test]
    public void ConfiguratorPrefersCliThenEnvironmentOverPersistedSettings()
    {
        var active = new Configuration.ActiveConfig
        {
            OodleLibrary = "/stored/library.dll",
            WineExecutable = "/stored/wine",
            OodleHelper = "/stored/helper.exe"
        };
        var options = new CliOptions { OodleLibrary = "/cli/library.dll" };
        var environment = new Dictionary<string, string?>
        {
            ["WITCHY_OODLE_LIBRARY"] = "/environment/library.dll",
            ["WITCHY_WINE"] = "/environment/wine",
            ["WITCHY_OODLE_HELPER"] = "/environment/helper.exe"
        };

        OodleBackendSettings settings = OodleBackendConfigurator.ResolveSettings(
            options, active, name => environment.GetValueOrDefault(name));

        Assert.That(settings.Library, Is.EqualTo("/cli/library.dll"));
        Assert.That(settings.Wine, Is.EqualTo("/environment/wine"));
        Assert.That(settings.Helper, Is.EqualTo("/environment/helper.exe"));
    }

    [Test]
    public void DoctorReportsMissingDependenciesWithoutLeakingConfiguredPaths()
    {
        var options = new CliOptions
        {
            WineExecutable = "/private/user/wine",
            OodleLibrary = "/private/game/oo2core_6_win64.dll",
            OodleHelper = "/private/app/helper.exe"
        };

        OodleDoctorReport report = OodleDoctor.Inspect(options, _ => false, _ => null);
        string output = string.Join('\n', report.Lines);

        Assert.That(report.Ready, Is.False);
        Assert.That(output, Does.Contain("Wine: missing"));
        Assert.That(output, Does.Contain("User Oodle DLL: missing"));
        Assert.That(output, Does.Not.Contain("/private/"));
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void DoctorExitCodeReflectsBackendReadiness(bool ready, int expected)
    {
        var report = new OodleDoctorReport(ready, Array.Empty<string>());

        Assert.That(OodleDoctor.ExitCode(report), Is.EqualTo(expected));
    }

    [Test]
    public void WineBackendUsesBoundedTemporaryFilesAndCleansThem()
    {
        string root = Path.Combine(Path.GetTempPath(), $"witchy-wine-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string wine = CreateFile(root, "wine");
        string helper = CreateFile(root, "helper.exe");
        string library = CreateFile(root, "oo2core_6_win64.dll");
        var options = new WineOodleOptions(
            wine,
            helper,
            library,
            OodleVersion.Oodle6,
            Path.Combine(root, "work"),
            TimeSpan.FromSeconds(2));

        try
        {
            var backend = new WineOodleBackend(options, (_, arguments, _, _) =>
            {
                if (!OperatingSystem.IsWindows())
                {
                    Assert.That(File.GetUnixFileMode(options.WorkingRoot), Is.EqualTo(
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute));
                    Assert.That(File.GetUnixFileMode(Path.GetDirectoryName(arguments[4])!), Is.EqualTo(
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute));
                }
                Assert.That(arguments[1], Is.EqualTo("6"));
                Assert.That(arguments[3], Is.EqualTo("decompress"));
                File.WriteAllBytes(arguments[5], [1, 2, 3]);
                return new ProcessInvocationResult(0, "", "");
            });

            byte[] output = backend.Decompress([9], 3);

            Assert.That(output, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(Directory.EnumerateFileSystemEntries(options.WorkingRoot), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void WineBackendSurfacesHelperFailureWithoutLeavingInputs()
    {
        string root = Path.Combine(Path.GetTempPath(), $"witchy-wine-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var options = new WineOodleOptions(
            CreateFile(root, "wine"),
            CreateFile(root, "helper.exe"),
            CreateFile(root, "oo2core_6_win64.dll"),
            OodleVersion.Oodle6,
            Path.Combine(root, "work"),
            TimeSpan.FromSeconds(2));

        try
        {
            var backend = new WineOodleBackend(options,
                (_, _, _, _) => new ProcessInvocationResult(9, "", "decode failed"));

            InvalidOperationException? exception = Assert.Throws<InvalidOperationException>(() =>
                backend.Decompress([9], 3));

            Assert.That(exception!.Message, Does.Contain("decode failed"));
            Assert.That(Directory.EnumerateFileSystemEntries(options.WorkingRoot), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    [Category("NativeMacOoz")]
    public void NativeOozCompressionIsOptInAndDoesNotExposeDecompression()
    {
        string library = Path.Combine(AppContext.BaseDirectory, "libwitchy_ooz.dylib");
        using var backend = new NativeOozCompressionBackend(library);

        Assert.That(backend.IsAvailable, Is.True, $"Missing native test library: {library}");
        Assert.Throws<NotSupportedException>(() => backend.Decompress([1], 1));
    }

    [TestCase(130_036)]
    [TestCase(307_636)]
    [TestCase(67_960_282)]
    [Category("NativeMacOoz")]
    public void NativeOozCompressionProducesBoundedKrakenStreams(int size)
    {
        string library = Path.Combine(AppContext.BaseDirectory, "libwitchy_ooz.dylib");
        using var backend = new NativeOozCompressionBackend(library);
        byte[] input = new byte[size];
        for (int index = 0; index < input.Length; index++)
            input[index] = (byte)(index * 31);

        byte[] compressed = backend.Compress(
            input,
            SoulsFormats.Oodle.OodleLZ_Compressor.OodleLZ_Compressor_Kraken,
            SoulsFormats.Oodle.OodleLZ_CompressionLevel.OodleLZ_CompressionLevel_Normal);

        Assert.That(compressed, Is.Not.Empty);
        Assert.That(compressed.Length, Is.LessThanOrEqualTo(input.Length + 65_536));
    }

    [Test]
    [Category("NativeMacOoz")]
    public void NativeOozCompressionSupportsConcurrentIndependentCalls()
    {
        string library = Path.Combine(AppContext.BaseDirectory, "libwitchy_ooz.dylib");
        using var backend = new NativeOozCompressionBackend(library);

        byte[][] results = Enumerable.Range(0, 4).AsParallel().Select(seed =>
        {
            byte[] input = Enumerable.Range(0, 130_036).Select(index => (byte)(index + seed)).ToArray();
            return backend.Compress(
                input,
                SoulsFormats.Oodle.OodleLZ_Compressor.OodleLZ_Compressor_Kraken,
                SoulsFormats.Oodle.OodleLZ_CompressionLevel.OodleLZ_CompressionLevel_Normal);
        }).ToArray();

        Assert.That(results, Has.All.Not.Empty);
    }

    private static string CreateFile(string root, string name)
    {
        string path = Path.Combine(root, name);
        File.WriteAllBytes(path, []);
        return path;
    }

    private sealed class FakeBackend(byte[] decompressed) : IOodleBackend
    {
        public string Name => "fake";
        public OodleBackendKind Kind => OodleBackendKind.Native;
        public bool IsAvailable { get; set; } = true;

        public byte[] Compress(
            byte[] source,
            SoulsFormats.Oodle.OodleLZ_Compressor compressor,
            SoulsFormats.Oodle.OodleLZ_CompressionLevel level) => [7];

        public byte[] Decompress(byte[] source, long uncompressedSize) => decompressed;
    }
}
