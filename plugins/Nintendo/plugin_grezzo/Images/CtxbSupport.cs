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
            // composed of dataType and PixelFormat
            // short+short
            [0x14016752] = ImageFormats.Rgba8888(),
            [0x80336752] = ImageFormats.Rgba4444(),
            [0x80346752] = ImageFormats.Rgba5551(),
            [0x14016754] = ImageFormats.Rgb888(),
            [0x83636754] = ImageFormats.Rgb565(),
            [0x14016756] = ImageFormats.A8(),
            [0x67616756] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x14016757] = ImageFormats.L8(),
            [0x67616757] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x67606758] = ImageFormats.La44(),
            [0x14016758] = ImageFormats.La88(),
            [0x0000675A] = ImageFormats.Etc1(true),
            [0x0000675B] = ImageFormats.Etc1A4(true),
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(CtxbFormats.ToDictionary(x => (int)x.Key, y => y.Value));

            return definition;
        }
    }
}
