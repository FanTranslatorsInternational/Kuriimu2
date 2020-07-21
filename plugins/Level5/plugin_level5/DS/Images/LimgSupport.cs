using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;

namespace plugin_level5.DS.Images
{
    class LimgHeader
    {
        [FixedLength(4)]
        public string magic;

        public uint paletteOffset;

        public short unkOffset1;
        public short unkCount1;     //Size 0x8
        public short unkOffset2;
        public short unkCount2;     //Size 0xc

        public short tileDataOffset;
        public short tileEntryCount;    //Size 0x2
        public short imageDataOffset;
        public short imageTileCount;    //Size 0x40

        public short imgFormat;
        public short colorCount;
        public short width;
        public short height;

        public short paddedWidth;
        public short paddedHeight;
    }

    class LimgSupport
    {
        public static IDictionary<int, (IIndexEncoding, IList<int>)> LimgFormats = new Dictionary<int, (IIndexEncoding, IList<int>)>
        {
            [0] = (new Index(4), new List<int> { 0 }),
            [1] = (new Index(8), new List<int> { 0 }),
            [2] = (new Index(5, 3), new List<int> { 0 })
        };

        public static IDictionary<int, IColorEncoding> LimgPaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(5, 5, 5, "BGR")
        };
    }
}
