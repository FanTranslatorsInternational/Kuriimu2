using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_grezzo.Images
{
    struct CtxbHeader
    {
        public string magic; // ctxb
        public int fileSize;
        public long chunkCount;
        public int chunkOffset;
        public int texDataOffset;
    }

    struct CtxbChunk
    {
        public string magic; // 'tex '
        public int chunkSize;
        public int texCount;
        public CtxbEntry[] textures;
    }

    class CtxbEntry
    {
        public int dataLength;
        public short mipLvl;
        public bool isETC1;
        public bool isCubemap;
        public short width;
        public short height;
        public ushort imageFormat;
        public ushort dataType;
        public int dataOffset;
        public string name;
    }

    class CtxbImageFileInfo : ImageFileInfo
    {
        public int ChunkIndex { get; }

        public CtxbEntry Entry { get; }

        public CtxbImageFileInfo(int chunkIndex, CtxbEntry entry)
        {
            ChunkIndex = chunkIndex;
            Entry = entry;
        }
    }

    public class CtxbSupport
    {
        public static readonly IDictionary<uint, IColorEncoding> CtxbFormats = new Dictionary<uint, IColorEncoding>
        {
            // Pixel Format:
            // - 0x6752: RGBA
            // - 0x6754: RGB
            // - 0x6756: A
            // - 0x6757: L
            // - 0x6758: LA
            // - 0x675A: ETC1
            // - 0x675B: ETC1A4

            // Data Format:
            // - 0x0000: default/unnecessary
            // - 0x1401: 8 per component
            // - 0x8033: 4444
            // - 0x8034: 5551
            // - 0x8363: 565
            // - 0x6761: 4
            // - 0x6760: 44

            // Composed of dataType and PixelFormat
            // Short + short
            [0x14016752] = ImageFormats.Rgba8888(),
            [0x00006752] = ImageFormats.Rgba8888(),
            [0x80336752] = ImageFormats.Rgba4444(),
            [0x80346752] = ImageFormats.Rgba5551(),
            [0x14016754] = ImageFormats.Rgb888(),
            [0x83636754] = ImageFormats.Rgb565(),
            [0x14016756] = ImageFormats.A8(),
            [0x67616756] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x14016757] = ImageFormats.L8(),
            [0x67616757] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x14016758] = ImageFormats.La88(),
            [0x67606758] = ImageFormats.La44(),
            [0x0000675A] = ImageFormats.Etc1(true),
            [0x0000675B] = ImageFormats.Etc1A4(true),
            [0x1401675A] = ImageFormats.Etc1(true),
            [0x1401675B] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(CtxbFormats.ToDictionary(x => unchecked((int)x.Key), y => y.Value));

            return definition;
        }
    }
}
