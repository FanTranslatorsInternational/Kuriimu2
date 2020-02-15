using System;
using System.Runtime.InteropServices;

namespace Kanvas.Encoding.BlockCompressions.BCn.Models
{
    /// <summary>
    /// The BC1 format.
    /// </summary>
    /// <remarks>
    /// This is also used by the BC2 and BC3 formats.
    /// </remarks>
    [Serializable]
    public struct BC1Block
    {
        public ulong PackedValue;

        public const ulong TransparentValue = 0xFFFFFFFFFFFF0000UL;

        public Rgb565 Rgb0
        {
            get { return new Rgb565((ushort)PackedValue); }
            set { PackedValue = (PackedValue & ~0xFFFFUL) | value.PackedValue; }
        }

        public Rgb565 Rgb1
        {
            get { return new Rgb565((ushort)(PackedValue >> 16)); }
            set { PackedValue = (PackedValue & ~0xFFFF0000UL) | ((ulong)value.PackedValue << 16); }
        }

        public bool HasTransparentValues
        {
            get { return (ushort)PackedValue <= (ushort)(PackedValue >> 16); }
        }

        public BC1Block(Rgb565 r0, Rgb565 r1)
        {
            PackedValue = ((ulong)r1.PackedValue << 16) | r0.PackedValue;
        }

        public void GetPalette(Rgb565[] palette, int index = 0)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (palette.Length - index < 4)
                throw new ArgumentOutOfRangeException("index");

            var c0 = Rgb0;
            var c1 = Rgb1;

            palette[index + 0] = c0;
            palette[index + 1] = c1;

            if (HasTransparentValues)
            {
                var interp = new Rgb565();

                interp.R = (c0.R + c1.R) / 2;
                interp.G = (c0.G + c1.G) / 2;
                interp.B = (c0.B + c1.B) / 2;

                palette[index + 2] = interp;

                palette[index + 3] = new Rgb565(0);
            }
            else
            {
                var interp = new Rgb565();

                interp.R = (c0.R * 2 + c1.R) / 3;
                interp.G = (c0.G * 2 + c1.G) / 3;
                interp.B = (c0.B * 2 + c1.B) / 3;

                palette[index + 2] = interp;

                interp.R = (c0.R + c1.R * 2) / 3;
                interp.G = (c0.G + c1.G * 2) / 3;
                interp.B = (c0.B + c1.B * 2) / 3;

                palette[index + 3] = interp;
            }
        }

        public void GetPalette(float[] rPalette, float[] gPalette,
            float[] bPalette, float[] aPalette = null, int index = 0)
        {
            if (rPalette == null)
                throw new ArgumentNullException("rPalette");
            if (gPalette == null)
                throw new ArgumentNullException("gPalette");
            if (bPalette == null)
                throw new ArgumentNullException("bPalette");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (rPalette.Length - index < 4 || gPalette.Length - index < 4 ||
                bPalette.Length - index < 4 || (aPalette != null && aPalette.Length - index < 4))
                throw new ArgumentOutOfRangeException("index");

            var c0 = Rgb0;
            var c1 = Rgb1;

            var r0 = c0.RF;
            var g0 = c0.GF;
            var b0 = c0.BF;

            var r1 = c1.RF;
            var g1 = c1.GF;
            var b1 = c1.BF;

            rPalette[index + 0] = r0;
            gPalette[index + 0] = g0;
            bPalette[index + 0] = b0;

            rPalette[index + 1] = r1;
            gPalette[index + 1] = g1;
            bPalette[index + 1] = b1;

            if (HasTransparentValues)
            {
                rPalette[index + 2] = (r0 + r1) / 2;
                gPalette[index + 2] = (g0 + g1) / 2;
                bPalette[index + 2] = (b0 + b1) / 2;

                rPalette[index + 3] = 0;
                gPalette[index + 3] = 0;
                bPalette[index + 3] = 0;
            }
            else
            {
                rPalette[index + 2] = (r0 * 2 + r1) / 3;
                gPalette[index + 2] = (g0 * 2 + g1) / 3;
                bPalette[index + 2] = (b0 * 2 + b1) / 3;

                rPalette[index + 3] = (r0 + r1 * 2) / 3;
                gPalette[index + 3] = (g0 + g1 * 2) / 3;
                bPalette[index + 3] = (b0 + b1 * 2) / 3;
            }

            if (aPalette != null)
            {
                aPalette[index + 0] = 1;
                aPalette[index + 1] = 1;
                aPalette[index + 2] = 1;
                aPalette[index + 3] = HasTransparentValues ? 0 : 1;
            }
        }

        public void GetIndices(int[] indices, int index = 0)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (indices.Length - index < 16)
                throw new ArgumentOutOfRangeException("index");

            uint idxs = (uint)(PackedValue >> 32);
            for (int i = 0; i < 16; i++)
            {
                indices[index + i] = (int)(idxs & 0x3);
                idxs >>= 2;
            }
        }

        /// <summary>
        /// Gets the color index of a given pixel (linear address).
        /// </summary>
        /// <param name="index">The index of the pixel.</param>
        /// <returns>
        /// If <seealso cref="HasTransparentValues"/> is true, index <c>3</c>
        /// is the transparent index.
        /// </returns>
        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");

                var idx = (int)(PackedValue >> (32 + index * 2));

                return idx;
            }

            set
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");
                if (value < 0 || value > 3)
                    throw new ArgumentOutOfRangeException("value");

                int shift = 32 + index * 2;

                PackedValue = (PackedValue & ~(0x3UL << index)) |
                    ((ulong)value << index);
            }
        }

        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("x");

                return this[y * 4 + x];
            }

            set
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("x");

                this[y * 4 + x] = value;
            }
        }
    }

    /// <summary>
    /// The BC2 alpha block format.
    /// </summary>
    [Serializable]
    public struct BC2ABlock
    {
        public ulong PackedValue;

        public float this[int index]
        {
            get
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");

                return ((PackedValue >> (index * 4)) & 0xF) * (1F / 15F);
            }

            set
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");

                int v;
                if (value <= 0) v = 0;
                else if (value >= 1) v = 15;
                else v = (int)(value * 15F + 0.5F);

                int shift = index * 4;

                PackedValue = (PackedValue & ~(0xFU << shift)) | ((ulong)v << shift);
            }
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("x");

                return this[y * 4 + x];
            }

            set
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("x");

                this[y * 4 + x] = value;
            }
        }

        public void GetValues(float[] values, int index = 0)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (values.Length - index < 16)
                throw new ArgumentOutOfRangeException("index");

            ulong v = PackedValue;
            for (int i = 0; i < 16; i++)
            {
                values[index + i] = (v & 0xF) * 15F;
                v >>= 4;
            }
        }

        public void GetValues(float[] values, int index, int pitch)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (values.Length - index < 16)
                throw new ArgumentOutOfRangeException("index");

            int endOffset = index + pitch * 3 + 3;
            if (endOffset < 0 || endOffset > values.Length)
                throw new ArgumentOutOfRangeException();

            ulong v = PackedValue;
            for (int y = 0; y < 4; y++)
            {
                int idx = index + y * pitch;
                for (int x = 0; x < 4; x++)
                {
                    values[idx] = (v & 0xF) * 15F;
                    v >>= 4;
                }
            }
        }
    }

    /// <summary>
    /// The BC2 block format.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BC2Block
    {
        public BC2ABlock A;
        public BC1Block Rgb;
    };

    /// <summary>
    /// The BC3 block format.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BC3Block
    {
        public BC4UBlock A;
        public BC1Block Rgb;
    };

    /// <summary>
    /// The signed BC4 block format.
    /// </summary>
    /// <remarks>
    /// This is used by the signed BC5 format.
    /// </remarks>
    [Serializable]
    public struct BC4UBlock
    {
        public ulong PackedValue;

        public byte R0
        {
            get { return (byte)PackedValue; }
            set { PackedValue = (PackedValue & ~0xFFUL) | value; }
        }

        public byte R1
        {
            get { return (byte)(PackedValue >> 8); }
            set { PackedValue = (PackedValue & ~0xFF00UL) | ((ulong)value << 8); }
        }

        public byte R2 { get { return GetR(2); } }
        public byte R3 { get { return GetR(3); } }
        public byte R4 { get { return GetR(4); } }
        public byte R5 { get { return GetR(5); } }
        public byte R6 { get { return GetR(6); } }
        public byte R7 { get { return GetR(7); } }

        public byte GetR(int index)
        {
            if (index < 0 || index >= 8)
                throw new ArgumentOutOfRangeException("index");

            byte r0 = R0;
            if (index == 0)
                return r0;

            byte r1 = R1;
            if (index == 1)
                return r1;

            index--;

            if (r0 <= r1)
            {
                if (index == 5)
                    return 0x00;

                if (index == 6)
                    return 0xFF;

                return unchecked((byte)((r0 * (5 - index) + r1 * index) / 5));
            }

            return unchecked((byte)((r0 * (7 - index) + r1 * index) / 7));
        }

        /// <summary>
        /// Computes all of the R Values.
        /// </summary>
        public void GetPalette(byte[] palette, int index = 0)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            if (index < 0 || palette.Length - index < 8)
                throw new ArgumentOutOfRangeException("index");

            byte r0 = R0;
            byte r1 = R1;

            palette[index + 0] = r0;
            palette[index + 1] = r1;

            if (r0 <= r1)
            {
                palette[index + 2] = unchecked((byte)((r0 * 4 + r1 * 1) / 5));
                palette[index + 3] = unchecked((byte)((r0 * 3 + r1 * 2) / 5));
                palette[index + 4] = unchecked((byte)((r0 * 2 + r1 * 3) / 5));
                palette[index + 5] = unchecked((byte)((r0 * 1 + r1 * 4) / 5));

                palette[index + 6] = 0x00;
                palette[index + 7] = 0xFF;
            }
            else
            {
                palette[index + 2] = unchecked((byte)((r0 * 6 + r1 * 1) / 7));
                palette[index + 3] = unchecked((byte)((r0 * 5 + r1 * 2) / 7));
                palette[index + 4] = unchecked((byte)((r0 * 4 + r1 * 3) / 7));
                palette[index + 5] = unchecked((byte)((r0 * 3 + r1 * 4) / 7));
                palette[index + 6] = unchecked((byte)((r0 * 2 + r1 * 5) / 7));
                palette[index + 7] = unchecked((byte)((r0 * 1 + r1 * 6) / 7));
            }
        }

        /// <summary>
        /// Computes all of the R Values.
        /// </summary>
        public void GetPalette(float[] palette, int index = 0)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            if (index < 0 || palette.Length - index < 8)
                throw new ArgumentOutOfRangeException("index");

            byte sr0 = R0;
            byte sr1 = R1;

            float r0 = sr0 / 255F;
            float r1 = sr1 / 255F;

            palette[index + 0] = r0;
            palette[index + 1] = r1;

            if (r0 <= r1)
            {
                palette[index + 2] = (r0 * 4F + r1 * 1F) / 5F;
                palette[index + 3] = (r0 * 3F + r1 * 2F) / 5F;
                palette[index + 4] = (r0 * 2F + r1 * 3F) / 5F;
                palette[index + 5] = (r0 * 1F + r1 * 4F) / 5F;

                palette[index + 6] = 0F;
                palette[index + 7] = 1F;
            }
            else
            {
                palette[index + 2] = (r0 * 6F + r1 * 1F) / 7F;
                palette[index + 3] = (r0 * 5F + r1 * 2F) / 7F;
                palette[index + 4] = (r0 * 4F + r1 * 3F) / 7F;
                palette[index + 5] = (r0 * 3F + r1 * 4F) / 7F;
                palette[index + 6] = (r0 * 2F + r1 * 5F) / 7F;
                palette[index + 7] = (r0 * 1F + r1 * 6F) / 7F;
            }
        }

        public void GetIndices(int[] indices, int index = 0)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (indices.Length - index < 16)
                throw new ArgumentOutOfRangeException("index");

            ulong idxs = PackedValue >> 16;
            for (int i = 0; i < 16; i++)
            {
                indices[index + i] = (int)(idxs & 0x7);
                idxs >>= 3;
            }
        }

        /// <summary>
        /// Gets or sets individual block index entries.
        /// </summary>
        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");

                return (int)((PackedValue >> (16 + 3 * index)) & 0x7);
            }

            set
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");
                if (value < 0 || value > 0x7)
                    throw new ArgumentOutOfRangeException("value");

                int shift = 16 + 3 * index;
                PackedValue = (PackedValue & ~(0x7UL << shift)) | ((ulong)value << shift);
            }
        }

        /// <summary>
        /// Gets or sets the block index for a given coordinate within the block.
        /// </summary>
        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("y");

                return (int)((PackedValue >> (16 + 3 * (y * 4 + x))) & 0x7);
            }

            set
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("y");

                int shift = 16 + 3 * (y * 4 + x);
                PackedValue = (PackedValue & ~(0x7UL << shift)) | ((ulong)value << shift);
            }
        }
    }

    /// <summary>
    /// The unsigned BC4 block format.
    /// </summary>
    /// <remarks>
    /// This is used by the unsigned BC5 format.
    /// </remarks>
    [Serializable]
    public struct BC4SBlock
    {
        public ulong PackedValue;

        public sbyte R0
        {
            get
            {
                var ret = (sbyte)PackedValue;
                return ret != -128 ? ret : (sbyte)-127;
            }
            set
            {
                if (value == -128)
                    value = -127;

                PackedValue = (PackedValue & ~0xFFUL) | (byte)value;
            }
        }

        public sbyte R1
        {
            get
            {
                var ret = (sbyte)(PackedValue >> 8);
                return ret != -128 ? ret : (sbyte)-127;
            }
            set
            {
                if (value == -128)
                    value = -127;

                PackedValue = (PackedValue & ~0xFF00UL) | ((ulong)(byte)value << 8);
            }
        }

        public sbyte R2 { get { return GetR(2); } }
        public sbyte R3 { get { return GetR(3); } }
        public sbyte R4 { get { return GetR(4); } }
        public sbyte R5 { get { return GetR(5); } }
        public sbyte R6 { get { return GetR(6); } }
        public sbyte R7 { get { return GetR(7); } }

        public sbyte GetR(int index)
        {
            if (index < 0 || index >= 8)
                throw new ArgumentOutOfRangeException("index");

            sbyte r0 = R0;
            if (index == 0)
                return r0;

            sbyte r1 = R1;
            if (index == 1)
                return r1;

            index--;

            if (r0 <= r1)
            {
                if (index == 5)
                    return -127;

                if (index == 6)
                    return 127;

                return unchecked((sbyte)((r0 * (5 - index) + r1 * index) / 5));
            }

            return unchecked((sbyte)((r0 * (7 - index) + r1 * index) / 7));
        }

        /// <summary>
        /// Computes all of the R Values.
        /// </summary>
        public void GetPalette(sbyte[] palette, int index = 0)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            if (index < 0 || palette.Length - index < 8)
                throw new ArgumentOutOfRangeException("index");

            sbyte r0 = R0;
            sbyte r1 = R1;

            palette[index + 0] = r0;
            palette[index + 1] = r1;

            if (r0 <= r1)
            {
                palette[index + 2] = unchecked((sbyte)((r0 * 4 + r1 * 1) / 5));
                palette[index + 3] = unchecked((sbyte)((r0 * 3 + r1 * 2) / 5));
                palette[index + 4] = unchecked((sbyte)((r0 * 2 + r1 * 3) / 5));
                palette[index + 5] = unchecked((sbyte)((r0 * 1 + r1 * 4) / 5));

                palette[index + 6] = -127;
                palette[index + 7] = 127;
            }
            else
            {
                palette[index + 2] = unchecked((sbyte)((r0 * 6 + r1 * 1) / 7));
                palette[index + 3] = unchecked((sbyte)((r0 * 5 + r1 * 2) / 7));
                palette[index + 4] = unchecked((sbyte)((r0 * 4 + r1 * 3) / 7));
                palette[index + 5] = unchecked((sbyte)((r0 * 3 + r1 * 4) / 7));
                palette[index + 6] = unchecked((sbyte)((r0 * 2 + r1 * 5) / 7));
                palette[index + 7] = unchecked((sbyte)((r0 * 1 + r1 * 6) / 7));
            }
        }

        /// <summary>
        /// Computes all of the R Values.
        /// </summary>
        public void GetPalette(float[] palette, int index = 0)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            if (index < 0 || palette.Length - index < 8)
                throw new ArgumentOutOfRangeException("index");

            sbyte sr0 = R0;
            sbyte sr1 = R1;

            float r0 = sr0 / 127F;
            float r1 = sr1 / 127F;

            palette[index + 0] = r0;
            palette[index + 1] = r1;

            if (r0 <= r1)
            {
                palette[index + 2] = (r0 * 4F + r1 * 1F) / 5F;
                palette[index + 3] = (r0 * 3F + r1 * 2F) / 5F;
                palette[index + 4] = (r0 * 2F + r1 * 3F) / 5F;
                palette[index + 5] = (r0 * 1F + r1 * 4F) / 5F;

                palette[index + 6] = -1F;
                palette[index + 7] = 1F;
            }
            else
            {
                palette[index + 2] = (r0 * 6F + r1 * 1F) / 7F;
                palette[index + 3] = (r0 * 5F + r1 * 2F) / 7F;
                palette[index + 4] = (r0 * 4F + r1 * 3F) / 7F;
                palette[index + 5] = (r0 * 3F + r1 * 4F) / 7F;
                palette[index + 6] = (r0 * 2F + r1 * 5F) / 7F;
                palette[index + 7] = (r0 * 1F + r1 * 6F) / 7F;
            }
        }

        public void GetIndices(int[] indices, int index = 0)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (indices.Length - index < 16)
                throw new ArgumentOutOfRangeException("index");

            ulong idxs = PackedValue >> 16;
            for (int i = 0; i < 16; i++)
            {
                indices[index + i] = (int)(idxs & 0x7);
                idxs >>= 3;
            }
        }

        /// <summary>
        /// Gets or sets individual block index entries.
        /// </summary>
        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");

                return (int)((PackedValue >> (16 + 3 * index)) & 0x7);
            }

            set
            {
                if (index < 0 || index >= 16)
                    throw new ArgumentOutOfRangeException("index");
                if (value < 0 || value > 0x7)
                    throw new ArgumentOutOfRangeException("value");

                int shift = 16 + 3 * index;
                PackedValue = (PackedValue & ~(0x7UL << shift)) | ((ulong)value << shift);
            }
        }

        /// <summary>
        /// Gets or sets the block index for a given coordinate within the block.
        /// </summary>
        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("y");

                return (int)((PackedValue >> (16 + 3 * (y * 4 + x))) & 0x7);
            }

            set
            {
                if (x < 0 || x >= 4)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= 4)
                    throw new ArgumentOutOfRangeException("y");

                int shift = 16 + 3 * (y * 4 + x);
                PackedValue = (PackedValue & ~(0x7UL << shift)) | ((ulong)value << shift);
            }
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BC5SBlock
    {
        public BC4SBlock R, G;
    }

    /// <summary>
    /// The BC5 block format.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BC5UBlock
    {
        public BC4UBlock R, G;
    }

    [Serializable]
    public struct Rgb565
    {
        public ushort PackedValue;

        public Rgb565(ushort packedValue)
        {
            this.PackedValue = packedValue;
        }

        public Rgb565(int r, int g, int b)
        {
            PackedValue = (ushort)((r << 11) | (g << 5) | b);
        }

        public int R
        {
            get { return PackedValue >> 11; }
            set { PackedValue = (ushort)((PackedValue & 0x07FF) | (value << 11)); }
        }

        public int G
        {
            get { return (PackedValue >> 5) & 0x3F; }
            set { PackedValue = (ushort)((PackedValue & 0xF81F) | ((value & 0x3F) << 5)); }
        }

        public int B
        {
            get { return PackedValue & 0x1F; }
            set { PackedValue = (ushort)((PackedValue & 0xFFE0) | (value & 0x1F)); }
        }

        public float RF
        {
            get { return R / 31F; }
            set
            {
                int iv;

                if (value <= 0) iv = 0;
                else if (value >= 1) iv = 31;
                else iv = (int)(value * 31 + 0.5F);

                R = iv;
            }
        }

        public float GF
        {
            get { return G / 63F; }
            set
            {
                int iv;

                if (value <= 0) iv = 0;
                else if (value >= 1) iv = 63;
                else iv = (int)(value * 63 + 0.5F);

                G = iv;
            }
        }

        public float BF
        {
            get { return B / 31F; }
            set
            {
                int iv;

                if (value <= 0) iv = 0;
                else if (value >= 1) iv = 31;
                else iv = (int)(value * 31 + 0.5F);

                B = iv;
            }
        }

        public static Rgb565 Pack(float r, float g, float b)
        {
            int ir, ig, ib;

            if (r <= 0) ir = 0;
            else if (r >= 1) ir = 31;
            else ir = (int)(r * 31 + 0.5F);

            if (g <= 0) ig = 0;
            else if (g >= 1) ig = 63;
            else ig = (int)(g * 63 + 0.5F);

            if (b <= 0) ib = 0;
            else if (b >= 1) ib = 31;
            else ib = (int)(b * 31 + 0.5F);

            return new Rgb565(ir, ig, ib);
        }

        internal static Rgb565 Pack(RgbF32 cl)
        {
            return Pack(cl.R, cl.G, cl.B);
        }

        internal void Unpack(out RgbF32 cl)
        {
            cl.R = RF;
            cl.G = GF;
            cl.B = BF;
        }
    }
}
