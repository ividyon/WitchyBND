using System.Runtime.InteropServices;
using SoulsFormats;
using SystemNativeLibrary = System.Runtime.InteropServices.NativeLibrary;

namespace SoulsOodleLib;

public sealed class NativeOozCompressionBackend : IOodleBackend, IDisposable
{
    private readonly string _libraryPath;
    private readonly IntPtr _handle;
    private readonly MaxCompressedSizeDelegate? _maxCompressedSize;
    private readonly CompressDelegate? _compress;

    public NativeOozCompressionBackend(string libraryPath)
    {
        _libraryPath = Path.GetFullPath(libraryPath);
        if (!File.Exists(_libraryPath))
            return;

        _handle = SystemNativeLibrary.Load(_libraryPath);
        _maxCompressedSize = Marshal.GetDelegateForFunctionPointer<MaxCompressedSizeDelegate>(
            SystemNativeLibrary.GetExport(_handle, "WitchyOoz_MaxCompressedSize"));
        _compress = Marshal.GetDelegateForFunctionPointer<CompressDelegate>(
            SystemNativeLibrary.GetExport(_handle, "WitchyOoz_Compress"));
    }

    public string Name => "experimental native ooz compression";
    public OodleBackendKind Kind => OodleBackendKind.OpenSourceCompression;
    public bool IsAvailable => _handle != IntPtr.Zero && _maxCompressedSize != null && _compress != null;

    public byte[] Compress(
        byte[] source,
        SoulsFormats.Oodle.OodleLZ_Compressor compressor,
        SoulsFormats.Oodle.OodleLZ_CompressionLevel level)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("The experimental native ooz compression library is unavailable.");
        if (source.Length == 0)
            return [];
        if ((int)compressor is not (8 or 9 or 11 or 13))
            throw new NotSupportedException($"The native ooz backend does not support compressor {(int)compressor}.");

        int levelValue = (int)level;
        if (levelValue < -4)
            throw new ArgumentOutOfRangeException(nameof(level), "The native ooz compression level must be at least -4.");
        if (_maxCompressedSize!((nuint)source.Length, out nuint maxSize) != 0 || maxSize > int.MaxValue)
            throw new InvalidDataException("The native ooz backend rejected the requested input size.");

        byte[] output = new byte[(int)maxSize];
        GCHandle sourceHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
        GCHandle outputHandle = GCHandle.Alloc(output, GCHandleType.Pinned);
        try
        {
            int written = _compress!(
                sourceHandle.AddrOfPinnedObject(),
                (nuint)source.Length,
                outputHandle.AddrOfPinnedObject(),
                maxSize,
                (int)compressor,
                levelValue);
            if (written <= 0 || written > output.Length)
                throw new InvalidDataException($"The native ooz backend returned an invalid output size: {written}.");
            Array.Resize(ref output, written);
            return output;
        }
        finally
        {
            outputHandle.Free();
            sourceHandle.Free();
        }
    }

    public byte[] Decompress(byte[] source, long uncompressedSize) =>
        throw new NotSupportedException(
            "Open-source Oodle decompression is disabled because it has not passed the compatibility and safety gates. Configure Wine and a user-owned Oodle DLL.");

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
            SystemNativeLibrary.Free(_handle);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MaxCompressedSizeDelegate(nuint inputSize, out nuint result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CompressDelegate(
        IntPtr input,
        nuint inputSize,
        IntPtr output,
        nuint outputCapacity,
        int compressor,
        int level);
}

public sealed class SplitOodleBackend(IOodleBackend decompressor, IOodleBackend compressor) : IOodleBackend
{
    public string Name => $"{decompressor.Name} + {compressor.Name}";
    public OodleBackendKind Kind => OodleBackendKind.OpenSourceCompression;
    public bool IsAvailable => decompressor.IsAvailable && compressor.IsAvailable;

    public byte[] Compress(
        byte[] source,
        SoulsFormats.Oodle.OodleLZ_Compressor compressorType,
        SoulsFormats.Oodle.OodleLZ_CompressionLevel level) =>
        compressor.Compress(source, compressorType, level);

    public byte[] Decompress(byte[] source, long uncompressedSize) =>
        decompressor.Decompress(source, uncompressedSize);
}
