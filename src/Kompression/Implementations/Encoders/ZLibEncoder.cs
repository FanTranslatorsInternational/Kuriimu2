using System.IO;
using System.IO.Compression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class ZlibEncoder : IEncoder
    {
        private readonly CompressionLevel _compressionLevel;

        public ZlibEncoder(CompressionLevel compressionLevel)
        {
            _compressionLevel = compressionLevel;
        }

        public void Encode(Stream input, Stream output)
        {
            using var zlib = new DeflateStream(output, _compressionLevel, true);
            input.CopyTo(zlib);
        }

        public void Dispose()
        {
        }
    }
}
