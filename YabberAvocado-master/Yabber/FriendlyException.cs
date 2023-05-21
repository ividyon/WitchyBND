using System;

namespace Yabber
{
    class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }
}
