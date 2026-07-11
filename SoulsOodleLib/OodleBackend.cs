using SoulsFormats;

namespace SoulsOodleLib;

public enum OodleBackendKind
{
    Unavailable,
    Native,
    Wine,
    OpenSourceCompression
}

public interface IOodleBackend : IOodleCompressor
{
    string Name { get; }
    OodleBackendKind Kind { get; }
    bool IsAvailable { get; }
}

public static class OodleBackendRegistry
{
    private static IOodleBackend? _current;

    public static IOodleBackend? Current
    {
        get => _current;
        set
        {
            _current = value;
            SoulsFormats.Oodle.CustomCompressorFactory = value == null ? null : CreateValidatedBackend;
        }
    }

    private static IOodleCompressor CreateValidatedBackend()
    {
        IOodleBackend backend = _current
            ?? throw new InvalidOperationException("No Oodle backend is configured.");
        if (!backend.IsAvailable)
            throw new InvalidOperationException($"The configured Oodle backend '{backend.Name}' is unavailable.");
        return new ValidatingOodleBackend(backend);
    }

    private sealed class ValidatingOodleBackend(IOodleBackend inner) : IOodleCompressor
    {
        public byte[] Compress(
            byte[] source,
            SoulsFormats.Oodle.OodleLZ_Compressor compressor,
            SoulsFormats.Oodle.OodleLZ_CompressionLevel level)
        {
            byte[] compressed = inner.Compress(source, compressor, level);
            if (compressed.Length == 0 && source.Length != 0)
                throw new InvalidDataException($"Oodle backend '{inner.Name}' returned empty compressed data.");
            return compressed;
        }

        public byte[] Decompress(byte[] source, long uncompressedSize)
        {
            if (uncompressedSize is < 0 or > int.MaxValue)
                throw new InvalidDataException($"Invalid Oodle output size: {uncompressedSize}.");

            byte[] decompressed = inner.Decompress(source, uncompressedSize);
            if (decompressed.LongLength != uncompressedSize)
            {
                throw new InvalidDataException(
                    $"Oodle backend '{inner.Name}' returned {decompressed.LongLength} bytes; expected {uncompressedSize}.");
            }
            return decompressed;
        }
    }
}
