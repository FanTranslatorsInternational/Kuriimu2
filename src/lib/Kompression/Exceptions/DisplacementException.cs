using System;
using System.Runtime.Serialization;

namespace Kompression.Exceptions
{
    /// <summary>
    /// Exception for displacements that go beyond the existing window buffer.
    /// </summary>
    [Serializable]
    public class DisplacementException : Exception
    {
        /// <summary>
        /// The displacement from the current position.
        /// </summary>
        public int Displacement { get; }

        /// <summary>
        /// The number of units already written.
        /// </summary>
        public long WrittenBytes { get; }

        /// <summary>
        /// The current position in the output.
        /// </summary>
        public long CurrentPosition { get; }

        /// <summary>
        /// Creates a new instance of <see cref="DisplacementException"/>.
        /// </summary>
        public DisplacementException(int displacement, long writtenBytes, long currentPosition) : base("Cannot go back more than already written.")
        {
            Displacement = displacement;
            WrittenBytes = writtenBytes;
            CurrentPosition = currentPosition;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DisplacementException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the displacement error.</param>
        public DisplacementException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DisplacementException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the displacement error.</param>
        /// <param name="inner">The inner exception thrown.</param>
        public DisplacementException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected DisplacementException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            info.AddValue(nameof(Displacement), Displacement);
            info.AddValue(nameof(WrittenBytes), WrittenBytes);
            info.AddValue(nameof(CurrentPosition), CurrentPosition);
        }
    }
}
