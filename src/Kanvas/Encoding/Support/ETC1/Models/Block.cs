using System.Runtime.InteropServices;

namespace Kanvas.Encoding.Support.ETC1.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Block
    {
        public ushort LSB { get; set; }
        public ushort MSB { get; set; }
        public byte Flags { get; set; }
        public byte B { get; set; }
        public byte G { get; set; }
        public byte R { get; set; }

        public bool FlipBit
        {
            get => (Flags & 1) == 1;
            set => Flags = (byte)((Flags & ~1) | (value ? 1 : 0));
        }
        public bool DiffBit
        {
            get => (Flags & 2) == 2;
            set => Flags = (byte)((Flags & ~2) | (value ? 2 : 0));
        }
        public int ColorDepth => DiffBit ? 32 : 16;
        public int Table0
        {
            get => (Flags >> 5) & 7;
            set => Flags = (byte)((Flags & ~(7 << 5)) | (value << 5));
        }
        public int Table1
        {
            get => (Flags >> 2) & 7;
            set => Flags = (byte)((Flags & ~(7 << 2)) | (value << 2));
        }
        public int this[int i] => (MSB >> i) % 2 * 2 + (LSB >> i) % 2;

        public RGB Color0 => new RGB(R * ColorDepth / 256, G * ColorDepth / 256, B * ColorDepth / 256);

        public RGB Color1
        {
            get
            {
                if (!DiffBit) return new RGB(R % 16, G % 16, B % 16);
                var c0 = Color0;
                int rd = Sign3(R % 8), gd = Sign3(G % 8), bd = Sign3(B % 8);
                return new RGB(c0.R + rd, c0.G + gd, c0.B + bd);
            }
        }

        private static int Sign3(int n) => (n + 4) % 8 - 4;
    }
}
