using System;
using System.IO;
using System.Text;

namespace Kryptography.Hash
{
    /// <summary>
    /// Exposes methods to compute a hash over given input data.
    /// </summary>
    public interface IHash
    {
        /// <summary>
        /// Computes a hash from a string encoded in ASCII.
        /// </summary>
        /// <param name="input">The string to compute the hash to.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(string input);

        /// <summary>
        /// Computes a hash from a string.
        /// </summary>
        /// <param name="input">The string to compute the hash to.</param>
        /// <param name="enc">The encoding the string should be encoded in.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(string input, Encoding enc);

        /// <summary>
        /// Computes a hash over a stream of data.
        /// </summary>
        /// <param name="input">The stream of data to hash.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(Stream input);

        /// <summary>
        /// Computes a hash over an array of bytes.
        /// </summary>
        /// <param name="input">The array of bytes to hash.</param>
        /// <returns>The computed hash.</returns>
        byte[] Compute(Span<byte> input);
    }
}
