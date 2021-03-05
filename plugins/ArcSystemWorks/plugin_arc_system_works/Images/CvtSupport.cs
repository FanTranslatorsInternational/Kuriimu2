using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_arc_system_works.Images
{
    class CvtHeader
    {
        [FixedLength(2)]
        public string magic="n\0";

        public short width;
        public short height;
        public short format;
        public int unk1;
        [FixedLength(0x20)]
        public string name;

        public int unk2;
        public int unk3;
    }

    public class CvtSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x1006] = new Etc1(true, true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
