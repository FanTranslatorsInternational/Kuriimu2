using System.Runtime.InteropServices;

namespace Kanvas.Encoding.BlockCompression.Etc1.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Block
    {
        public ushort Lsb { get; set; }
        public ushort Msb { get; set; }
        public byte Flags { get; set; }
        public byte B { get; set; }
        public byte G { get; set; }
        public byte R { get; set; }

        public readonly ulong GetBlockData()
        {
            var colorBlock = 0UL;
            colorBlock |= Lsb;
            colorBlock |= (ulong)Msb << 16;
            colorBlock |= (ulong)Flags << 32;
            colorBlock |= (ulong)B << 40;
            colorBlock |= (ulong)G << 48;
            colorBlock |= (ulong)R << 56;

            return colorBlock;
        }

        public readonly int this[int i] => (Msb >> i) % 2 * 2 + (Lsb >> i) % 2;

        public bool FlipBit
        {
            readonly get => (Flags & 1) == 1;
            set => Flags = (byte)((Flags & ~1) | (value ? 1 : 0));
        }

        public bool DiffBit
        {
            readonly get => (Flags & 2) == 2;
            set => Flags = (byte)((Flags & ~2) | (value ? 2 : 0));
        }

        public readonly int ColorDepth => DiffBit ? 32 : 16;

        public int Table0
        {
            readonly get => (Flags >> 5) & 7;
            set => Flags = (byte)((Flags & ~(7 << 5)) | (value << 5));
        }

        public int Table1
        {
            readonly get => (Flags >> 2) & 7;
            set => Flags = (byte)((Flags & ~(7 << 2)) | (value << 2));
        }

        public readonly Rgb Color0 => new(R * ColorDepth / 256, G * ColorDepth / 256, B * ColorDepth / 256);

        public readonly Rgb Color1
        {
            get
            {
                if (!DiffBit) return new Rgb(R % 16, G % 16, B % 16);
                var c0 = Color0;
                int rd = Sign3(R % 8), gd = Sign3(G % 8), bd = Sign3(B % 8);
                return new Rgb(c0.R + rd, c0.G + gd, c0.B + bd);
            }
        }

        private static int Sign3(int n) => (n + 4) % 8 - 4;
    }
}
