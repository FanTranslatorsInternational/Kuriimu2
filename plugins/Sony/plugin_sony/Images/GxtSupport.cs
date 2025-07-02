using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_sony.Images
{
    #region Gxt Structures

    class GxtFile
    {
        public GxtHeader header;

        [VariableLength("header.texCount")]
        [TypeChoice("header.version", TypeChoiceComparer.Equal, 0x10000001, typeof(GxtEntry1[]))]
        [TypeChoice("header.version", TypeChoiceComparer.Equal, 0x10000002, typeof(GxtEntry2[]))]
        [TypeChoice("header.version", TypeChoiceComparer.Equal, 0x10000003, typeof(GxtEntry3[]))]
        public IGxtEntry[] entries;
    }

    class GxtHeader
    {
        public string magic;
        public uint version;
        public int texCount;
        public int dataOffset;
        public int dataSize;
        public int p4PalCount;
        public int p8PalCount;
        public int zero0;
    }

    interface IGxtEntry
    {
        int DataOffset { get; set; }
        int DataSize { get; set; }
        int PaletteIndex { get; set; }
        int Flags { get; set; }

        int Width { get; set; }
        int Height { get; set; }

        int Type { get; set; }
        int Format { get; set; }
        int SubFormat { get; set; }
    }

    class GxtEntry1 : IGxtEntry
    {
        public int dataOffset;
        public int dataSize;
        public int paletteIndex;
        public int flags;

        public int unk1;
        public int tmp1;
        public int type;
        public int unk2;

        public int DataOffset { get => dataOffset; set => dataOffset = value; }
        public int DataSize { get => dataSize; set => dataSize = value; }
        public int PaletteIndex { get => paletteIndex; set => paletteIndex = value; }
        public int Flags { get => flags; set => flags = value; }

        public int Width { get => 1 << ((tmp1 >> 16) & 0xF); set => tmp1 = (int)(tmp1 & 0xFFF0FFFF) | ((int)Math.Log(value, 2) << 16); }
        public int Height { get => 1 << (tmp1 & 0xF); set => tmp1 = (int)(tmp1 & 0xFFFFFFF0) | (int)Math.Log(value, 2); }

        public int Type { get => type; set => type = (int)value; }
        public int Format { get => (int)(0x80000000 | (tmp1 & 0x0F000000)); set => tmp1 = (int)(tmp1 & 0xF0FFFFFF) | (value & 0x0F000000); }
        public int SubFormat { get; set; }
    }

    class GxtEntry2 : IGxtEntry
    {
        public int dataOffset;
        public int dataSize;
        public int paletteIndex;
        public int flags;

        public int unk1;
        public int tmp1;
        public int type;
        public int unk2;

        public int DataOffset { get => dataOffset; set => dataOffset = value; }
        public int DataSize { get => dataSize; set => dataSize = value; }
        public int PaletteIndex { get => paletteIndex; set => paletteIndex = value; }
        public int Flags { get => flags; set => flags = value; }

        public int Width { get => 1 << ((tmp1 >> 16) & 0xF); set => tmp1 = (int)(tmp1 & 0xFFF0FFFF) | ((int)Math.Log(value, 2) << 16); }
        public int Height { get => 1 << (tmp1 & 0xF); set => tmp1 = (int)(tmp1 & 0xFFFFFFF0) | (int)Math.Log(value, 2); }

        public int Type { get => type; set => type = (int)value; }
        public int Format { get => (int)(0x80000000 | (tmp1 & 0x0F000000)); set => tmp1 = (int)(tmp1 & 0xF0FFFFFF) | (value & 0x0F000000); }
        public int SubFormat { get; set; }
    }

    class GxtEntry3 : IGxtEntry
    {
        public int dataOffset;
        public int dataSize;
        public int paletteIndex;
        public int flags;

        public int type;
        public int format;
        public short width;
        public short height;
        public byte mipCount;

        [FixedLength(3)]
        public byte[] padding;

        public int DataOffset { get => dataOffset; set => dataOffset = value; }
        public int DataSize { get => dataSize; set => dataSize = value; }
        public int PaletteIndex { get => paletteIndex; set => paletteIndex = value; }
        public int Flags { get => flags; set => flags = value; }

        public int Width { get => width; set => width = (short)value; }
        public int Height { get => height; set => height = (short)value; }

        public int Type { get => type; set => type = (int)value; }
        public int Format { get => (int)(format & 0xFF000000); set => format = (format & 0x00FFFFFF) | (int)(value & 0xFF000000); }
        public int SubFormat { get => format & 0xFFFF; set => format = (int)(format & 0xFFFF0000) | value; }
    }

    #endregion

    class GxtSupport
    {
        public static readonly IDictionary<uint, IColorEncoding> Formats = new Dictionary<uint, IColorEncoding>
        {
            [0x85000000] = ImageFormats.Dxt1()
        };

        public static readonly IDictionary<uint, IIndexEncoding> IndexFormats = new Dictionary<uint, IIndexEncoding>
        {
            [0x94000000] = ImageFormats.I4(),
            [0x95000000] = ImageFormats.I8()
        };

        public static readonly IDictionary<uint, IColorEncoding> PaletteFormats = new Dictionary<uint, IColorEncoding>
        {
            [0x0000] = new Rgba(8, 8, 8, 8, "ABGR"),
            [0x1000] = new Rgba(8, 8, 8, 8, "ARGB"),
            [0x2000] = new Rgba(8, 8, 8, 8, "RGBA"),
            [0x3000] = new Rgba(8, 8, 8, 8, "BGRA"),
            [0x4000] = new Rgba(8, 8, 8, 8, "XBGR"),
            [0x5000] = new Rgba(8, 8, 8, 8, "XRGB"),
            [0x6000] = new Rgba(8, 8, 8, 8, "RGBX"),
            [0x7000] = new Rgba(8, 8, 8, 8, "BGRX")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats.Select(x => ((int)x.Key, x.Value)).ToArray());

            definition.AddPaletteEncodings(PaletteFormats.Select(x => ((int)x.Key, x.Value)).ToArray());
            definition.AddIndexEncodings(IndexFormats.Select(x => ((int)x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = [..PaletteFormats.Keys.Select(x => (int)x)]
            })).ToArray());

            return definition;
        }
    }
}
