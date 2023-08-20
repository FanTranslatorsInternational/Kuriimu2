using System;
using System.Runtime.Serialization;

namespace Kompression.Exceptions
{
    /// <summary>
    /// Exception for streams that are not compressed with a certain compression.
    /// </summary>
    [Serializable]
    public class InvalidCompressionException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="InvalidCompressionException"/>.
        /// </summary>
        /// <param name="compressionName">Name of the compression tried to use.</param>
        public InvalidCompressionException(string compressionName) : base($"This stream is not compressed with '{compressionName}'.")
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="InvalidCompressionException"/>.
        /// </summary>
        /// <param name="message">A message detailing the error.</param>
        /// <param name="inner">The inner exception thrown.</param>
        public InvalidCompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected InvalidCompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
