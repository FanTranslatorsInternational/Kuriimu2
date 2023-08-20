using System;
using System.Runtime.Serialization;

namespace Kompression.Exceptions
{
    /// <summary>
    /// Exception for streams that are too short to be decompressed correctly.
    /// </summary>
    [Serializable]
    public class StreamTooShortException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="StreamTooShortException"/>.
        /// </summary>
        public StreamTooShortException():base("Stream is too short.")
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="StreamTooShortException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the short stream.</param>
        public StreamTooShortException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="StreamTooShortException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the short stream.</param>
        /// <param name="inner">The inner exception thrown.</param>
        public StreamTooShortException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected StreamTooShortException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
