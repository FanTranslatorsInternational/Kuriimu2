using System;

namespace Kompression.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when you try to read beyond the beginning of the stream.
    /// </summary>
    public class BeginningOfStreamException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginningOfStreamException" /> class with a specified error message.
        /// </summary>
        public BeginningOfStreamException() : base("Reached the beginning of the reversed stream.") { }
    }
}
