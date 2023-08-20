using System;
using System.Collections.Generic;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.IO;
using Kontract.Models.Plugins.State.Image;

namespace plugin_level5.DS.Images
{
    class GtxtLtHeader
    {
        [FixedLength(4)]
        public string magic = "GTXT";
        public byte indexFormat;
        public byte unk1;
        public byte unk2;
        public byte unk3;

        public short paddedWidth;
        public short paddedHeight;
        public short width;
        public short height;

        public short unkOffset1;
        public short unkCount1;

        public short unkOffset2;
        public short unkCount2;

        public short indexOffset;
        public short indexCount;

        public int dataOffset;
        public short tileCount;
        public short unk4;
    }

    class GtxtLpHeader
    {
        [FixedLength(4)]
        public string magic = "GPLT";
        public short colorCount;
        public short paletteFormat;
    }

    class GtxtSupport
    {
        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [2] = ImageFormats.I4(BitOrder.LeastSignificantBitFirst),
            [3] = ImageFormats.I8()
        };

        public static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [8] = new Rgba(5, 5, 5, 1, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddPaletteEncodings(PaletteFormats);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new EncodingDefinition.IndexEncodingDefinition(x.Value, new List<int> { 8 }))).ToArray());

            return definition;
        }

        public static int ToPowerOfTwo(int value)
        {
            return 2 << (int)Math.Log(value - 1, 2);
        }
    }
}
