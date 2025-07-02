using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_spike_chunsoft.Images
{
    class SrdHeader
    {
        public string magic;
        public int sectionSize;
        public int subDataSize;
        public int unk1;
    }

    class SrdSection
    {
        public SrdHeader header;
        public byte[] sectionData;
        public byte[] subData;
    }

    class SrdImageFileInfo : ImageFileInfo
    {
        public required SrdSection Section { get; init; }
    }

    class SrdSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x0F] = ImageFormats.Dxt1(),
            [0x11] = ImageFormats.Dxt5(),
            [0x14] = ImageFormats.Ati2(),
            [0x16] = ImageFormats.Ati1(),
            [0x1C] = ImageFormats.Bc7()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
