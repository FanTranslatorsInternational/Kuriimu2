using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_grezzo.Fonts
{
    public class QbfHeader
    {
        public string magic;
        public short entryCount;
        public short glyphCount;
        public int unk2;
        public byte bitsPerPixel;
        public byte glyphWidth;
        public byte glyphHeight;
        public byte imgFormat;
    }

    public class QbfEntry
    {
        public short codePoint;
        public short index;
        public byte posX;
        public byte charWidth;
        public short unk3;
    }

    class QbfSupport
    {
        public static Dictionary<int, IColorEncoding> Formats = new()
        {
            [2] = ImageFormats.A4(),
            [4] = ImageFormats.La44()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
