using System.Diagnostics;

namespace Level5.Fonts.Models
{
    [DebuggerDisplay("[Offset: {offsetX},{offsetY}; Glyph: {glyphWidth},{glyphHeight}]")]
    public class XfCharSizeInfo
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
    }
}
