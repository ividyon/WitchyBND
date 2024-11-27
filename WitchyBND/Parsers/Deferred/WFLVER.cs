using System;
using System.IO;
using System.Xml.Linq;
using SoulsFormats;

namespace WitchyBND.Parsers;

public class WFLVER : WDeferredFileParser
{
    public override string Name => "FLVER";
    public override string[] UnpackExtensions => new[] { ".flver" };
    public override string[] RepackExtensions => Array.Empty<string>();
    public override DeferFormat DeferFormat => DeferFormat.Flver;
}