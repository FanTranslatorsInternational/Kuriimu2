using System.IO;
using System.IO.Compression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class DeflateEncoder : IEncoder
    {
        public void Encode(Stream input, Stream output)
        {
            new DeflateStream(input, CompressionLevel.Optimal).CopyTo(output);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
