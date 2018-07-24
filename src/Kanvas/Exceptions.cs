using System;
using System.Runtime.Serialization;

namespace Kanvas
{
    /// <summary>
    /// Exception thrown by the Processor of Kanvas.
    /// </summary>
    [Serializable]
    public class ImageFormatException : Exception
    {
        public ImageFormatException() { }
        public ImageFormatException(string message) : base(message) { }
        public ImageFormatException(string message, Exception innerException) : base(message, innerException) { }
        protected ImageFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
