using System;

namespace WitchyBND.Parsers;

public class WHKX : WDeferredFileParser
{
    public override string Name => "HKX";
    public override string[] UnpackExtensions => new[] { ".hkx" };
    public override string[] RepackExtensions => Array.Empty<string>();
    public override DeferFormat DeferFormat => DeferFormat.Hkx;
}