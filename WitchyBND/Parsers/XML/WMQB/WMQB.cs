﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPlus;
using SoulsFormats;
using MQB = WitchyFormats.MQB;

namespace WitchyBND.Parsers;

public partial class WMQB : WXMLParser
{

    public override string Name => "MQB";

    public override string XmlTag => "MQB";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MQB>(path, data, out file);
    }
}