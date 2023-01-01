using System;
using System.IO;

namespace Kontract.Kompression.Interfaces.Configuration
{
    /// <summary>
    /// Provides functionality to encode data.
    /// </summary>
    public interface IEncoder : IDisposable
    {
        /// <summary>
        /// Encodes a stream of data.
        /// </summary>
        /// <param name="input">The input data to encode.</param>
        /// <param name="output">The output to encode to.</param>
        void Encode(Stream input, Stream output);
    }
}
