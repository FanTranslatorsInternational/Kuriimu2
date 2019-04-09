using Kanvas.Swizzle;
using Komponent.IO.Attributes;

namespace plugin_nintendo.BCLIM
{
    public class BclimHeader
    {
        [FixedLength(4)]
        public string Magic;
        public int SectionSize;
        public short Width;
        public short Height;
        public byte Format;
        public CTRSwizzle.Transformation SwizzleTileMode; // Not used in BCLIM
        public short Alignment;
        public int DataSize;
    }
}
