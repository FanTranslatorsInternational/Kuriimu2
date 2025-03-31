namespace plugin_nintendo.Common.Compression
{
    enum NintendoCompressionMethod : byte
    {
        Lz10 = 0x10,
        Lz11 = 0x11,
        Lz40 = 0x40,
        Lz60 = 0x60,
        Huffman4 = 0x24,
        Huffman8 = 0x28,
        Rle = 0x30,
        
        Unsupported = 0xFF
    }
}