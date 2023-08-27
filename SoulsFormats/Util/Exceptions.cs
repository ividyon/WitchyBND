using System;

namespace SoulsFormats;

public class NoOodleFoundException : Exception
{
    public NoOodleFoundException(string message) : base(message) { }
}
