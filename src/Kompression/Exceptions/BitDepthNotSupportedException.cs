using System;
using System.Runtime.Serialization;
using Kompression.Huffman;

namespace Kompression.Exceptions
{
    /// <summary>
    /// Exception for an invalid bit depth in an <see cref="IHuffmanTreeBuilder"/>.
    /// </summary>
    [Serializable]
    public class BitDepthNotSupportedException : Exception
    {
        /// <summary>
        /// The bit depth that is not supported.
        /// </summary>
        public int BitDepth { get; }

        /// <summary>
        /// Creates a new instance of <see cref="BitDepthNotSupportedException"/>.
        /// </summary>
        /// <param name="bitDepth">The unsupported bit depth.</param>
        public BitDepthNotSupportedException(int bitDepth) : base($"BitDepth {bitDepth} not supported.")
        {
            BitDepth = bitDepth;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitDepthNotSupportedException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the bit depth error.</param>
        public BitDepthNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitDepthNotSupportedException"/>.
        /// </summary>
        /// <param name="message">A message describing details about the bit depth error.</param>
        /// <param name="inner">The inner exception thrown.</param>
        public BitDepthNotSupportedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BitDepthNotSupportedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            info.AddValue(nameof(BitDepth), BitDepth);
        }
    }
}
