using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPlus;
using WitchyFormats;

namespace WitchyBND.Parsers;

public partial class WMQB : WXMLParser
{

    public override string Name => "MQB";

    public override bool Is(string path)
    {
        return MQB.Is(path);
    }
}