using System;

namespace WitchyLib;
    public class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }

    [Serializable]
    public class UnsupportedActionException : Exception
    {
        public UnsupportedActionException(string message) : base(message) {}
        public UnsupportedActionException(string message, Exception e) : base(message, e) {}
    }

    [Serializable]
    public class RegulationOutOfBoundsException : Exception {
        public RegulationOutOfBoundsException(string message) : base(message) {}
        public RegulationOutOfBoundsException(string message, Exception e) : base(message, e) {}
    }

    [Serializable]
    public class MalformedBinderException : Exception {
        public MalformedBinderException(string message) : base(message) {}
        public MalformedBinderException(string message, Exception e) : base(message, e) {}
    }
