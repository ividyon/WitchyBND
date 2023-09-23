using System;

namespace WitchyLib
{
    public class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }

    [Serializable]
    public class IntendedException : Exception
    {
        public IntendedException(string message) : base(message)
        {

        }
    }
}
