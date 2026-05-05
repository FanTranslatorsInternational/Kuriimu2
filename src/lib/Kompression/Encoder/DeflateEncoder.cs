using System.IO.Compression;
using Kompression.Contract.Encoder;

namespace Kompression.Encoder
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
            GC.SuppressFinalize(this);
        }
    }
}
