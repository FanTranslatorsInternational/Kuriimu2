using System;
using System.Collections.Generic;
using System.Text;

namespace Level5.Fonts.Compression
{
    public enum CompressionMethod
    {
        NoCompression = 0,
        LZ10 = 1,
        Huffman4Bit = 2,
        Huffman8Bit = 3,
        RLE = 4
    }
}
