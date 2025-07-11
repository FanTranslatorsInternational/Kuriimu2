using EasyCompressor;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kompression.Contract.Encoder;

namespace Kompression.Encoder
{
    public class LZMAEncoder : IEncoder
    {
        public void Encode(Stream input, Stream output)
        {
            var lzma = new LZMACompressor();
            lzma.Compress(input, output);
        }

        public void Dispose()
        {
        }
    }
}
