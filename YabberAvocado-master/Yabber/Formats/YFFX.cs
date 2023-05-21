using SoulsFormats;
using System.IO;

namespace Yabber
{
    static class YFFX
    {
        public static void Unpack(this FFXDLSE ffx, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            using (var sw = new StreamWriter(targetFile))
                ffx.XmlSerialize(sw);
        }

        public static void Repack(string sourceFile)
        {
            FFXDLSE ffx;
            using (var sr = new StreamReader(sourceFile))
                ffx = FFXDLSE.XmlDeserialize(sr);

            string outPath = sourceFile.Replace(".ffx.xml", ".ffx").Replace(".ffx.dcx.xml", ".ffx.dcx");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            ffx.Write(outPath);
        }
    }
}
