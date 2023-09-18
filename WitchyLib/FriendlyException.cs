using System;

namespace WitchyLib
{
    public class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }
}
