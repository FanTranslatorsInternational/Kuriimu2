using EasyCompressor;
using Kompression.Contract.Encoder;

namespace Kompression.Encoder
{
    public class LzmaEncoder : IEncoder
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
