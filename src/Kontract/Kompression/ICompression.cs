using System;
using System.IO;

namespace Kontract.Kompression
{
    /// <summary>
    /// Provides functionality to compress or decompress data.
    /// </summary>
    public interface ICompression : IDisposable
    {
        /// <summary>
        /// Names for the used compression.
        /// </summary>
        string[] Names { get; }

        /// <summary>
        /// Decompress a stream of data.
        /// </summary>
        /// <param name="input">The input data to decompress.</param>
        /// <param name="output">The output to decompress to.</param>
        void Decompress(Stream input, Stream output);

        /// <summary>
        /// Compresses a stream of data.
        /// </summary>
        /// <param name="input">The input data to compress.</param>
        /// <param name="output">The output to compress to.</param>
        void Compress(Stream input, Stream output);
    }
}
