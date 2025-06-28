using Kanvas.Contract.Encoding;
using Kanvas;
using Kanvas.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_bandai_namco.Images
{
    public class MtexHeader
    {      
        public string magic = "XETM";
        public int unk1;
        public int unk2;
        public short unk3;
        public short width;
        public short height;
        public short unk4;
        public short format;        
    }

    public static class MtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> MtexFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Etc1(false, true),
            [0x01] = new Etc1(true, true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(MtexFormats);

            return definition;
        }
    }
}
