using System;
using System.IO;

namespace Kontract.Kompression.Configuration
{
    /// <summary>
    /// Provides functionality to decode data.
    /// </summary>
    public interface IDecoder : IDisposable
    {
        /// <summary>
        /// Decodes a stream of data.
        /// </summary>
        /// <param name="input">The input data to decode.</param>
        /// <param name="output">The output to decode to.</param>
        void Decode(Stream input, Stream output);
    }
}
