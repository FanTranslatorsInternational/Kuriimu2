using System.Diagnostics;

namespace Kanvas.Encoding.BlockCompression.Etc1.Models
{
    [DebuggerDisplay("{R},{G},{B}")]
    public readonly struct Rgb(int r, int g, int b)
    {
        public byte R { get; } = (byte)r;
        public byte G { get; } = (byte)g;
        public byte B { get; } = (byte)b;
        public byte Padding { get; } = 0; // padding for speed reasons

        private static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));
        private static int ErrorRgb(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception

        public static Rgb operator +(Rgb c, int mod) => new(Clamp(c.R + mod), Clamp(c.G + mod), Clamp(c.B + mod));
        public static int operator -(Rgb c1, Rgb c2) => ErrorRgb(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B);
        public static Rgb Average(Rgb[] src) => new((int)src.Average(c => c.R), (int)src.Average(c => c.G), (int)src.Average(c => c.B));
        public Rgb Scale(int limit) => limit == 16 ? new Rgb(R * 17, G * 17, B * 17) : new Rgb((R << 3) | (R >> 2), (G << 3) | (G >> 2), (B << 3) | (B >> 2));
        public Rgb Unscale(int limit) => new(R * limit / 256, G * limit / 256, B * limit / 256);

        public override int GetHashCode() => R | (G << 8) | (B << 16);
        public override bool Equals(object? obj) => obj != null && GetHashCode() == obj.GetHashCode();
        public static bool operator ==(Rgb c1, Rgb c2) => c1.Equals(c2);
        public static bool operator !=(Rgb c1, Rgb c2) => !c1.Equals(c2);
    }
}
