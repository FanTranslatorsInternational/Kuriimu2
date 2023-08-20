using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.Plugins.State.Image;

namespace plugin_level5.Switch.Images
{
    class NxtchHeader
    {
        [FixedLength(8)]
        public string magic = "NXTCH000";
        public int textureDataSize;
        public int unk1;
        public int unk2;
        public int width;
        public int height;
        public int unk3;
        public int unk4;
        public int format;
        public int mipMapCount;
        public int textureDataSize2;
    }

    class NxtchSupport
    {
        public static IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x25] = ImageFormats.Rgba8888(),
            [0x42] = ImageFormats.Dxt1(),
            [0x4D] = ImageFormats.Bc7()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
