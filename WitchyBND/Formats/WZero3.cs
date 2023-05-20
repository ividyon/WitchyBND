using SoulsFormats.AC4;
using System.IO;

namespace WitchyBND
{
    static class WZero3
    {
        public static void Unpack(this Zero3 z3, string targetDir)
        {
            foreach (Zero3.File file in z3.Files)
            {
                string outPath = $@"{targetDir}\{file.Name.Replace('/', '\\')}";
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllBytes(outPath, file.Bytes);
            }
        }
    }
}
