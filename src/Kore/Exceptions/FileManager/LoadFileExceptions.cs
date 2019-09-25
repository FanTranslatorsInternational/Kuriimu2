using System;
using System.Runtime.Serialization;

namespace Kore.Exceptions.FileManager
{
    /// <summary>
    /// Exception thrown by the LoadFile function of Kore.
    /// </summary>
    [Serializable]
    public class LoadFileException : Exception
    {
        public LoadFileException()
        {

        }

        public LoadFileException(string message) : base(message)
        {

        }

        public LoadFileException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected LoadFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
