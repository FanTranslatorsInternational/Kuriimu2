using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

#pragma warning disable 649

namespace plugin_level5.DS.Images
{
    class LimgHeader
    {
        [FixedLength(4)]
        public string magic;

        public uint paletteOffset;

        public short unkOffset1;
        public short unkCount1;     // Size 0x8
        public short unkOffset2;
        public short unkCount2;     // Size 0xc

        public short tileDataOffset;
        public short tileEntryCount;    // Size 0x2
        public short imageDataOffset;
        public short imageTileCount;    // Size 0x40

        public short imgFormat;
        public short colorCount;
        public short width;
        public short height;

        public short paddedWidth;
        public short paddedHeight;
    }

    class LimgSupport
    {
        public static IDictionary<int, IndexEncodingDefinition> LimgFormats = new Dictionary<int, IndexEncodingDefinition>
        {
            [0] = new IndexEncodingDefinition(new Index(4, ByteOrder.LittleEndian, BitOrder.LeastSignificantBitFirst), new[] { 0 }),
            [1] = new IndexEncodingDefinition(new Index(8), new[] { 0 }),
            [2] = new IndexEncodingDefinition(new Index(5, 3), new[] { 0 }),
        };

        public static IDictionary<int, IColorEncoding> LimgPaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(5, 5, 5, "BGR")
        };
    }
}
