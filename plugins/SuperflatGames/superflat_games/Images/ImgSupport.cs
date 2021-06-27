using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace superflat_games.Images
{
    class ImgHeader
    {
        [FixedLength(4)]
        public string magic;
        public int size;
        public int zero0;
    }

    class ImgEntry
    {
        public int width;
        public int height;
        public int zero0;
        public int zero1;
    }

    class ImgSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
