using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
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
        }
    }
}
