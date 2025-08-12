namespace WitchyBND.Parsers;

public class WGFX : WDeferredFileParser
{
    public override string Name => "GFX";
    public override string[] UnpackExtensions => new[] { ".gfx" };
    public override string[] RepackExtensions => new[] { ".gfx.xml" };
    public override DeferFormat DeferFormat => DeferFormat.Gfx;
}