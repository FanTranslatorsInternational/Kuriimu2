namespace Kanvas.Encoding.BlockCompressions.BCn
{
    internal static class Helpers
    {
        public static sbyte FloatToSNorm(float v)
        {
            //clamp

            if (v > 1) v = 1;
            else if (v < -1) v = -1;

            //scale and bias (so the rounding is correct)

            v *= 127;
            v += v >= 0 ? 0.5F : -0.5F;

            //round and return

            return (sbyte)v;
        }

        public static byte FloatToUNorm(float v)
        {
            //clamp

            if (v > 1) return 255;
            if (v < 0) return 0;

            //scale and truncate
            return (byte)(v * 255);
        }
    }

    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct RgbF32
    {
        public float R, G, B;

        public RgbF32(float r, float g, float b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public static void Lerp(out RgbF32 o, RgbF32 a, RgbF32 b, float t)
        {
            o.R = a.R + t * (b.R - a.R);
            o.G = a.G + t * (b.G - a.G);
            o.B = a.B + t * (b.B - a.B);
        }

    }
}
