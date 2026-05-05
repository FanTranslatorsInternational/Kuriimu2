using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kompression.Contract.Encoder;

namespace Kompression.Encoder
{
    public class ZlibEncoder : IEncoder
    {
        public void Encode(Stream input, Stream output)
        {
            using var zlib = new DeflaterOutputStream(output, new Deflater((int)Deflater.CompressionLevel.BEST_COMPRESSION)) { IsStreamOwner = false };
            input.CopyTo(zlib);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
