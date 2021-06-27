using System.IO;
using System.IO.Compression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class DeflateEncoder : IEncoder
    {
        public void Encode(Stream input, Stream output)
        {
            using var deflateStream = new DeflateStream(output, CompressionLevel.Optimal, true);
            input.CopyTo(deflateStream);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
