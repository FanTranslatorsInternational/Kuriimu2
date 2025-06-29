using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Aspects;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_blue_reflection.Images
{
    public class KsltHeader
    {
        public string Magic;
        public int FileCount;
        public int FileSize;
        public int OffsetTable;
        public int FNameTableSize;
        public int FileCount2;
    }

    public class ImageHeader
    {
        public int unk0;
        public short Width;
        public short Height;
        public int unk3;
        public int unk4;
        public int unk5;
        public int unk6;
        public int unk7;
        public int DataSize;
        public int unk8;
        [FixedLength(0x24)]
        public byte[] Padding;
    }

    class KsltImageFileInfo : ImageFileInfo
    {
        public ImageHeader Header { get; }

        public KsltImageFileInfo(ImageHeader header)
        {
            Header = header;
        }
    }

    static class KsltSupport
    {
        public static Dictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x0] = new Rgba(8, 8, 8, 8, "ARGB")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
