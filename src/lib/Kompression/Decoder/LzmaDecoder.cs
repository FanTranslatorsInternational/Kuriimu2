using EasyCompressor;
using Kompression.Contract.Decoder;

namespace Kompression.Decoder
{
    public class LzmaDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var lzma = new LZMACompressor();
            lzma.Decompress(input, output);
        }

        public void Dispose()
        {
        }
    }
}
