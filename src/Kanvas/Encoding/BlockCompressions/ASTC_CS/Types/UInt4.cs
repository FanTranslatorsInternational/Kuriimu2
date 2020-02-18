using System.Drawing;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Types
{
    struct UInt4
    {
        public int x;
        public int y;
        public int z;
        public int w;

        public UInt4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static UInt4 operator +(UInt4 value0, UInt4 value1) =>
            new UInt4(value0.x + value1.x, value0.y + value1.y, value0.z + value1.z, value0.w + value1.w);

        public static UInt4 operator -(UInt4 value0, UInt4 value1) =>
            new UInt4(value0.x - value1.x, value0.y - value1.y, value0.z - value1.z, value0.w - value1.w);

        public static UInt4 operator *(UInt4 value0, UInt4 value1) =>
            new UInt4(value0.x * value1.x, value0.y * value1.y, value0.z * value1.z, value0.w * value1.w);

        public static UInt4 operator >>(UInt4 value0, int shift) =>
            new UInt4(value0.x >> shift, value0.y >> shift, value0.z >> shift, value0.w >> shift);

        public static implicit operator UInt4(Color color) => new UInt4(color.R, color.G, color.B, color.A);
        public static implicit operator Color(UInt4 value) => Color.FromArgb(value.w, value.x, value.y, value.z);
    }
}
