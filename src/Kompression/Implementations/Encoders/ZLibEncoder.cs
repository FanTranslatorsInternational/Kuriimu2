using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class ZlibEncoder : IEncoder
    {
        public void Encode(Stream input, Stream output)
        {
            using var zlib = new DeflaterOutputStream(output) { IsStreamOwner = false };
            input.CopyTo(zlib);
        }

        public void Dispose()
        {
        }
    }
}
