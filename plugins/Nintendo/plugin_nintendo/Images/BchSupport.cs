using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    struct BchHeader
    {
        public string magic;
        public byte backwardComp;
        public byte forwardComp;
        public ushort version;

        public uint mainHeaderOffset;
        public uint nameTableOffset;
        public uint gpuCommandsOffset;
        public uint dataOffset;
        public uint dataExtOffset;
        public uint relocTableOffset;

        public uint mainHeaderSize;
        public uint nameTableSize;
        public uint gpuCommandsSize;
        public uint dataSize;
        public uint dataExtSize;
        public uint relocTableSize;

        public uint uninitDataSectionSize;
        public uint uninitDescSectionSize;

        public ushort flags;
        public ushort addressCount;
    }

    class BchSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var formats = new Dictionary<int, IColorEncoding>
            {
                [0] = ImageFormats.Rgba8888(),
                [1] = ImageFormats.Rgb888(),
                [2] = ImageFormats.Rgba5551(),
                [3] = ImageFormats.Rgb565(),
                [4] = ImageFormats.Rgba4444(),
                [5] = ImageFormats.La88(),
                [6] = ImageFormats.Rg88(),
                [7] = ImageFormats.L8(),
                [8] = ImageFormats.A8(),
                [9] = ImageFormats.La44(),
                [10] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
                [11] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
                [12] = ImageFormats.Etc1(true),
                [13] = ImageFormats.Etc1A4(true)
            };

            var definition = EncodingDefinition.Empty;
            definition.AddColorEncodings(formats);

            return definition;
        }
    }
}
