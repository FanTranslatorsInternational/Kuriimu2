using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class ZLibDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var zlib = new InflaterInputStream(input);
            zlib.CopyTo(output);
        }

        public void Dispose()
        {
        }
    }
}
