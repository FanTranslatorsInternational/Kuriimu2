using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kompression.Contract.Decoder;

namespace Kompression.Decoder
{
    public class ZLibDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            using var zlib = new InflaterInputStream(input) { IsStreamOwner = false };
            zlib.CopyTo(output);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
