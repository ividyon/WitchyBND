using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// An on-demand reader for BND3 containers.
    /// </summary>
    public class BND3Reader : BinderReader, IBND3
    {
        /// <summary>
        /// Unknown; always 0 except in DeS where it's occasionally 0x80000000 (probably a byte).
        /// </summary>
        public int Unk18 { get; set; }

        /// <summary>
        /// Type of compression used, if any.
        /// </summary>
        public DCX.Type Compression { get; set; }

        public int CompressionLevel { get; set; }

        /// <summary>
        /// Reads a BND3 from the given path, decompressing if necessary.
        /// </summary>
        public BND3Reader(string path)
        {
            FileStream fs = File.OpenRead(path);
            var br = new BinaryReaderEx(false, fs);
            Read(br);
        }

        /// <summary>
        /// Reads a BND3 from the given bytes, decompressing if necessary.
        /// </summary>
        public BND3Reader(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            var br = new BinaryReaderEx(false, ms);
            Read(br);
        }

        private void Read(BinaryReaderEx br)
        {
            br = SFUtil.GetDecompressedBR(br, out DCX.Type compression, out int compressionLevel);
            Compression = compression;
            CompressionLevel = compressionLevel;
            Files = BND3.ReadHeader(this, br);
            DataBR = br;
        }
    }
}
