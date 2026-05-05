using System.IO.Compression;
using Kompression.Contract.Decoder;

namespace Kompression.Decoder
{
    public class DeflateDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            new DeflateStream(input, CompressionMode.Decompress).CopyTo(output);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
