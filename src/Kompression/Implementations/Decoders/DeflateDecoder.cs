using System.IO;
using System.IO.Compression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class DeflateDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            new DeflateStream(input, CompressionMode.Decompress).CopyTo(output);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
