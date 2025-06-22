using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    public struct CtpkHeader
    {
        public string magic; // CTPK
        public short version;
        public short texCount;
        public int texSecOffset;
        public int texSecSize;
        public int crc32SecOffset;
        public int texInfoOffset;
    }

    public class TexEntry
    {
        public int nameOffset;
        public int texDataSize;
        public int texOffset;
        public int imageFormat;
        public short width;
        public short height;
        public byte mipLvl;
        public byte type;
        public short zero0;
        public int sizeOffset;
        public uint timeStamp;
    }

    public struct HashEntry
    {
        public uint crc32;
        public int id;
    }

    public class MipmapEntry
    {
        public byte mipmapFormat;
        public byte mipLvl;
        //never used compression specifications?
        public byte compression;
        public byte compMethod;
    }

    public class CtpkSupport
    {
        private static Dictionary<int, IColorEncoding> CtrFormat = new()
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

        public static EncodingDefinition GetEncodingDefinitions()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(CtrFormat);

            return encodingDefinition;
        }
    }

    class CtpkImageFileInfo : ImageFileInfo
    {
        public TexEntry Entry { get; init; }
        public MipmapEntry MipEntry { get; init; }
    }
}
