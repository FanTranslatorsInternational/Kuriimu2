using Komponent.IO.Attributes;

namespace Level5.Fonts.Models
{
    [BitFieldInfo(BlockSize = 2)]
    public class XfCharInformation
    {
        [BitField(6)]
        public int charWidth;
        [BitField(10)]
        public int charSizeInfoIndex;
    }
}
