using System;
using System.Runtime.InteropServices;

namespace SoulsFormats
{
    internal class Oodle28 : IOodleCompressor
    {
        public byte[] Compress(byte[] source, Oodle.OodleLZ_Compressor compressor, Oodle.OodleLZ_CompressionLevel level)
        {
            IntPtr pOptions = OodleLZ_CompressOptions_GetDefault();
            Oodle.OodleLZ_CompressOptions options = Marshal.PtrToStructure<Oodle.OodleLZ_CompressOptions>(pOptions);
            // Required for the game to not crash
            options.seekChunkReset = true;
            // This is already the default but I am including it for authenticity to game code
            options.seekChunkLen = 0x40000;
            pOptions = Marshal.AllocHGlobal(Marshal.SizeOf<Oodle.OodleLZ_CompressOptions>());

            try
            {
                Marshal.StructureToPtr(options, pOptions, false);
                long compressedBufferSizeNeeded = OodleLZ_GetCompressedBufferSizeNeeded(0, source.LongLength);
                byte[] compBuf = new byte[compressedBufferSizeNeeded];
                long compLen = OodleLZ_Compress(compressor, source, source.LongLength, compBuf, level, pOptions, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);
                Array.Resize(ref compBuf, (int)compLen);
                return compBuf;
            }
            finally
            {
                Marshal.FreeHGlobal(pOptions);
            }
        }

        public byte[] Decompress(byte[] source, long uncompressedSize)
        {
            long decodeBufferSize = OodleLZ_GetDecodeBufferSize(0, uncompressedSize, true);
            byte[] rawBuf = new byte[decodeBufferSize];
            long rawLen = OodleLZ_Decompress(source, source.LongLength, rawBuf, uncompressedSize);
            Array.Resize(ref rawBuf, (int)rawLen);
            return rawBuf;
        }


        /// <param name="compressor"></param>
        /// <param name="rawBuf"></param>
        /// <param name="rawLen"></param>
        /// <param name="compBuf"></param>
        /// <param name="level"></param>
        /// <param name="pOptions">= NULL</param>
        /// <param name="dictionaryBase">= NULL</param>
        /// <param name="lrm">= NULL</param>
        /// <param name="scratchMem">= NULL</param>
        /// <param name="scratchSize">= 0</param>
        [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern long OodleLZ_Compress(
            Oodle.OodleLZ_Compressor compressor,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] rawBuf,
            long rawLen,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] compBuf,
            Oodle.OodleLZ_CompressionLevel level,
            IntPtr pOptions,
            IntPtr dictionaryBase,
            IntPtr lrm,
            IntPtr scratchMem,
            long scratchSize);

        private static long OodleLZ_Compress(Oodle.OodleLZ_Compressor compressor, byte[] rawBuf, long rawLen, byte[] compBuf, Oodle.OodleLZ_CompressionLevel level)
            => OodleLZ_Compress(compressor, rawBuf, rawLen, compBuf, level,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);


        [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr OodleLZ_CompressOptions_GetDefault();

        /// <param name="compBuf"></param>
        /// <param name="compBufSize"></param>
        /// <param name="rawBuf"></param>
        /// <param name="rawLen"></param>
        /// <param name="fuzzSafe">= OodleLZ_FuzzSafe_Yes</param>
        /// <param name="checkCRC">= OodleLZ_CheckCRC_No</param>
        /// <param name="verbosity">= OodleLZ_Verbosity_None</param>
        /// <param name="decBufBase">= NULL</param>
        /// <param name="decBufSize">= 0</param>
        /// <param name="fpCallback">= NULL</param>
        /// <param name="callbackUserData">= NULL</param>
        /// <param name="decoderMemory">= NULL</param>
        /// <param name="decoderMemorySize">= 0</param>
        /// <param name="threadPhase">= OodleLZ_Decode_Unthreaded</param>
        [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern long OodleLZ_Decompress(
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] compBuf,
            long compBufSize,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] rawBuf,
            long rawLen,
            Oodle.OodleLZ_FuzzSafe fuzzSafe,
            Oodle.OodleLZ_CheckCRC checkCRC,
            Oodle.OodleLZ_Verbosity verbosity,
            IntPtr decBufBase,
            long decBufSize,
            IntPtr fpCallback,
            IntPtr callbackUserData,
            IntPtr decoderMemory,
            long decoderMemorySize,
            Oodle.OodleLZ_Decode_ThreadPhase threadPhase);

        private static long OodleLZ_Decompress(byte[] compBuf, long compBufSize, byte[] rawBuf, long rawLen)
            => OodleLZ_Decompress(compBuf, compBufSize, rawBuf, rawLen,
                Oodle.OodleLZ_FuzzSafe.OodleLZ_FuzzSafe_Yes, Oodle.OodleLZ_CheckCRC.OodleLZ_CheckCRC_No, Oodle.OodleLZ_Verbosity.OodleLZ_Verbosity_None,
                IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, Oodle.OodleLZ_Decode_ThreadPhase.OodleLZ_Decode_Unthreaded);

        /// <summary>
        /// Relevant Info: http://cbloomrants.blogspot.com/2019/04/oodle-280-release.html?m=1
        /// </summary>
        /// <param name="unkCompressorArg"> An unknown parameter related to the compressor that determines buffer size. 
        /// This is zero for our purposes. "Compressor argument to return smaller padding for the new codec"</param>
        /// <param name="rawSize"></param>
        /// <returns></returns>
        [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern long OodleLZ_GetCompressedBufferSizeNeeded(
            // It is possible that this arg just takes OodleLZ_Compressor enum as an argument and
            // gets truncated by the function by only using what is in `al`
            byte unkCompressorArg,
            long rawSize);


        /// <summary>
        /// Relevant Info: http://cbloomrants.blogspot.com/2019/04/oodle-280-release.html?m=1
        /// </summary>
        /// <param name="unkCompressorArg"> An unknown parameter related to the compressor that determines buffer size. 
        /// This is zero for our purposes. "Compressor argument to return smaller padding for the new codec" </param>
        /// <param name="rawSize"></param>
        /// <param name="corruptionPossible"></param>
        /// <returns></returns>
        [DllImport("oo2core_8_win64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern long OodleLZ_GetDecodeBufferSize(
            // It is possible that this arg just takes OodleLZ_Compressor enum as an argument and
            // gets truncated by the function by only using what is in `al`
            byte unkCompressorArg,
            long rawSize,
            [MarshalAs(UnmanagedType.Bool)]
            bool corruptionPossible);


    }
}
