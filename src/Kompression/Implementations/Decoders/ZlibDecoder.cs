using System.IO;
using System.IO.Compression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class ZLibDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var zlib = new DeflateStream(input, CompressionMode.Decompress, true);
            zlib.CopyTo(output);
        }

        public void Dispose()
        {
        }
    }
}
