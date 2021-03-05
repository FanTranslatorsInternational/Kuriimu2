using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_ganbarion.Images
{
    class JtexHeader
    {
        [FixedLength(4)] 
        public string magic = "jIMG";
        public int fileSize;
        public short paddedWidth;
        public short paddedHeight;
        public byte format;
        public byte orientation;
        public short unk1;
        public int unk2;
        public int unk3;
        public int dataSize;
        public int unk4;
        public int unk5;
        public int unk6;
        public int unk7;
        public short width;
        public short height;
    }

    class JtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [8] = new Etc1(false, true),
            [11] = new Etc1(true, true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
