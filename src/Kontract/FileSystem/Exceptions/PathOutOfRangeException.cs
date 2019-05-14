using System;
using System.Runtime.Serialization;

namespace Kontract.FileSystem.Exceptions
{
    [Serializable]
    public class PathOutOfRangeException : Exception
    {
        public PathOutOfRangeException(string path) : base($"\"{path}\" is outside the virtual space.")
        {
        }

        public PathOutOfRangeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PathOutOfRangeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
