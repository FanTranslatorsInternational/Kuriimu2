using System.Diagnostics.CodeAnalysis;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_level5._3DS.Fonts
{
    public class XfHeader
    {
        [FixedLength(8)]
        public string magic;
        public int version;
        public short largeCharHeight;
        public short smallCharHeight;
        public short largeEscapeCharacter;
        public short smallEscapeCharacter;
        public long zero0;

        public short charSizeOffset;
        public short charSizeCount;
        public short largeCharOffset;
        public short largeCharCount;
        public short smallCharOffset;
        public short smallCharCount;
    }

    class XfCharSizeInfo
    {
        public sbyte offsetX;
        public sbyte offsetY;
        public byte glyphWidth;
        public byte glyphHeight;

        public override bool Equals(object obj)
        {
            var csi = (XfCharSizeInfo)obj;
            return offsetX == csi?.offsetX && offsetY == csi.offsetY && glyphWidth == csi.glyphWidth && glyphHeight == csi.glyphHeight;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hash = 13;

            hash = hash * 7 + offsetX.GetHashCode();
            hash = hash * 7 + offsetY.GetHashCode();
            hash = hash * 7 + glyphWidth.GetHashCode();
            hash = hash * 7 + glyphHeight.GetHashCode();

            return hash;
        }
    }

    class XfCharMap
    {
        public ushort codePoint;
        public XfCharInformation charInformation;
        //public int imageInformation;
        public XfImageInformation imageInformation;
    }

    [BitFieldInfo(BlockSize = 2)]
    public class XfCharInformation
    {
        [BitField(6)]
        public int charWidth;
        [BitField(10)]
        public int charSizeInfoIndex;
    }

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
