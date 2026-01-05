using System;
using System.IO;
using SoulsFormats;
using SoulsFormats.Other;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WZERO3 : WFolderParser
{

    public override string Name => "Zero3";
    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        return OSPath.GetExtension(path) == ".000";
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override bool IsUnpacked(string path)
    {
        return false;
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        var z3 = Zero3.Read(srcPath);
        var targetDir = GetUnpackDestPath(srcPath, recursive);
        foreach (Zero3.File file in z3.Files)
        {
            string outPath = OSPath.Combine(targetDir, file.Name.ToOSPath());
            Directory.CreateDirectory(OSPath.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, file.Bytes);
        }
    }

    public override void Repack(string srcPath, bool recursive)
    {
        throw new NotSupportedException();
    }
}