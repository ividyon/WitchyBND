﻿using System;

namespace WitchyBND.Parsers;

public class WLUA : WDeferredFileParser
{
    public override string Name => "LUA";
    public override string[] UnpackExtensions => new[] { ".lua", ".hks" };
    public override string[] RepackExtensions => Array.Empty<string>();
    public override DeferFormat DeferFormat => DeferFormat.Lua;
}