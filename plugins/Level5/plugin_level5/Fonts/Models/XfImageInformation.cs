using Komponent.IO.Attributes;

namespace plugin_level5.Fonts.Models
{
    [BitFieldInfo(BlockSize = 4)]
    public class XfImageInformation
    {
        [BitField(14)]
        public int imageOffsetY;
        [BitField(14)]
        public int imageOffsetX;
        [BitField(4)]
        public int colorChannel;
    }
}
