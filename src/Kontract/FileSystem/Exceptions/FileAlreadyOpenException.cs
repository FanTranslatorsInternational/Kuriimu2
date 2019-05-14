using System;
using System.Runtime.Serialization;

namespace Kontract.FileSystem.Exceptions
{
    [Serializable]
    public class FileAlreadyOpenException : Exception
    {
        public FileAlreadyOpenException()
        {
        }

        public FileAlreadyOpenException(string message) : base(message)
        {
        }

        public FileAlreadyOpenException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FileAlreadyOpenException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
