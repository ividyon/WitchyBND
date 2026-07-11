using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using SoulsOodleLib;

namespace WitchyBND.Services;

public sealed record OodleDoctorReport(bool Ready, IReadOnlyList<string> Lines);

public static class OodleDoctor
{
    public static int ExitCode(OodleDoctorReport report) => report.Ready ? 0 : 1;

    public static OodleDoctorReport Inspect(
        CliOptions options,
        Func<string, bool>? fileExists = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        fileExists ??= File.Exists;
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        OodleBackendSettings settings = OodleBackendConfigurator.ResolveSettings(
            options, getEnvironmentVariable: getEnvironmentVariable);
        string? wine = WineDiscovery.Find(settings.Wine, fileExists, getEnvironmentVariable);
        string helper = settings.Helper;
        string? library = settings.Library;

        bool wineReady = wine != null;
        bool helperReady = fileExists(Path.GetFullPath(helper));
        bool libraryReady = library != null && fileExists(Path.GetFullPath(library));
        string libraryStatus = libraryReady ? ValidateLibrary(Path.GetFullPath(library!)) : "missing";
        string nativeLibrary = Path.GetFullPath(FirstNonEmpty(
            options.NativeOozLibrary,
            getEnvironmentVariable("WITCHY_NATIVE_OOZ_LIBRARY"),
            Path.Combine(AppContext.BaseDirectory, "libwitchy_ooz.dylib"))!);
        string nativeStatus = !options.NativeOozCompression
            ? "disabled"
            : fileExists(nativeLibrary) ? "enabled" : "missing";
        bool ready = wineReady && helperReady && libraryStatus == "configured (x64, exports valid)";

        return new OodleDoctorReport(ready,
        [
            $"Wine: {(wineReady ? "found" : "missing")}",
            $"Oodle helper: {(helperReady ? "found" : "missing")}",
            $"User Oodle DLL: {libraryStatus}",
            $"Native ooz compression: {nativeStatus}",
            $"Oodle backend: {(ready ? "ready" : "unavailable")}",
        ]);
    }

    private static string ValidateLibrary(string path)
    {
        try
        {
            OodleBackendConfigurator.ParseVersion(path);
            using FileStream stream = File.OpenRead(path);
            using var reader = new PEReader(stream);
            if (reader.PEHeaders.CoffHeader.Machine != Machine.Amd64)
                return "invalid architecture (expected x64)";
            HashSet<string> exports = ReadExports(stream, reader.PEHeaders);
            string[] required =
            [
                "OodleLZ_Decompress",
                "OodleLZ_Compress",
                "OodleLZ_GetCompressedBufferSizeNeeded",
                "OodleLZ_CompressOptions_GetDefault"
            ];
            return required.All(exports.Contains)
                ? "configured (x64, exports valid)"
                : "invalid Windows Oodle DLL (required exports missing)";
        }
        catch (Exception exception) when (exception is IOException or BadImageFormatException or InvalidDataException)
        {
            return "invalid Windows Oodle DLL";
        }
    }

    private static HashSet<string> ReadExports(Stream stream, PEHeaders headers)
    {
        int exportRva = headers.PEHeader?.ExportTableDirectory.RelativeVirtualAddress ?? 0;
        if (exportRva == 0)
            return [];

        using var binary = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
        stream.Position = RvaToOffset(exportRva, headers);
        binary.ReadBytes(24);
        uint nameCount = binary.ReadUInt32();
        binary.ReadUInt32();
        uint nameTableRva = binary.ReadUInt32();
        if (nameCount > 65_536)
            throw new InvalidDataException("PE export table is unreasonably large.");

        stream.Position = RvaToOffset(checked((int)nameTableRva), headers);
        uint[] nameRvas = new uint[nameCount];
        for (int index = 0; index < nameRvas.Length; index++)
            nameRvas[index] = binary.ReadUInt32();

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (uint nameRva in nameRvas)
        {
            stream.Position = RvaToOffset(checked((int)nameRva), headers);
            var bytes = new List<byte>();
            for (int index = 0; index < 512; index++)
            {
                byte value = binary.ReadByte();
                if (value == 0)
                    break;
                bytes.Add(value);
            }
            result.Add(System.Text.Encoding.ASCII.GetString(bytes.ToArray()));
        }
        return result;
    }

    private static long RvaToOffset(int rva, PEHeaders headers)
    {
        foreach (SectionHeader section in headers.SectionHeaders)
        {
            int size = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + size)
                return section.PointerToRawData + (rva - section.VirtualAddress);
        }
        throw new InvalidDataException("PE export RVA is outside the section table.");
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
