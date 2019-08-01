using System;
using System.IO;

namespace Kompression.Huffman.Decoders
{
    public interface IHuffmanDecoder : IDisposable
    {
        void Decode(Stream input, Stream output);
    }
}
