using System.Reflection;
using PPlus;

namespace WitchyBND.CliModes;

public class Config
{
    public static void CliConfigMode(CliOptions opt)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        PromptPlus.WriteLine(@$"{assembly.GetName().Name} has no GUI.
Drag and drop a file onto the exe to unpack it,
or an unpacked folder to repack it.

DCX files will be transparently decompressed and recompressed;
If you need to decompress or recompress an unsupported format,
use WitchyBND.DCX instead.

Press any key to exit.");
        PromptPlus.ReadKey();
    }
}