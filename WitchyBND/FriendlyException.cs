using System;

namespace WitchyBND
{
    class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }
}
