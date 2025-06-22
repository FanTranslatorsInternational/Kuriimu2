using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_arc_system_works.Images
{
    class CvtHeader
    {
        public string magic = "n\0";
        public short width;
        public short height;
        public short format;
        public int unk1;
        public string name;
        public int unk2;
        public int unk3;
    }

    public class CvtSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x1006] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
