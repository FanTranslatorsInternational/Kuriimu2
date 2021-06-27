using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_arc_system_works.Images
{
    class PicaRgHeader
    {
        [FixedLength(6)] 
        public string magic = "picaRg";
        public ushort format;
        public short width;
        public short height;
        public short paddedWidth;
        public short paddedHeight;
    }

    class PicaRgSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x1401] = ImageFormats.Rgba8888(),
            [0x8034] = ImageFormats.Rgba5551()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
