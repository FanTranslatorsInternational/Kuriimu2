using System;
using System.IO;

namespace Kryptography.Hash
{
    /// <summary>
    /// Exposes methods to compute a hash over given input data.
    /// </summary>
    public interface IHash
    {
        /// <summary>
        /// Computes a hash over an array of bytes.
        /// </summary>
        /// <param name="input">The array of bytes to hash.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(Span<byte> input);

        /// <summary>
        /// Computes a hash over a stream of data.
        /// </summary>
        /// <param name="input">The stream of data to hash.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(Stream input);
    }
}
