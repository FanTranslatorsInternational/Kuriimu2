using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the LA encoding.
    /// </summary>
    public class LA : IColorEncoding
    {
        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; set; }

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => false;

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; set; }

        /// <summary>
        /// The bit depth of the luminence component.
        /// </summary>
        public int LuminenceDepth { get; }

        /// <summary>
        /// The bit depth of the alpha component.
        /// </summary>
        public int AlphaDepth { get; }

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        /// <summary>
        /// Initializes a new instance of <see cref="LA"/>.
        /// </summary>
        /// <param name="l">Value of the luminence component.</param>
        /// <param name="a">Value of the alpha component.</param>
        public LA(int l, int a)
        {
            BitDepth = l + a;
            if (BitDepth % 4 != 0) throw new InvalidOperationException($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (BitDepth > 16) throw new InvalidOperationException($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (BitDepth < 4) throw new InvalidOperationException($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (l < 4 && a < 4) throw new InvalidOperationException($"Luminance and Alpha value can't be smaller than 4.\nGiven Luminance: {l}; Given Alpha: {a}");

            LuminenceDepth = l;
            AlphaDepth = a;

            UpdateName();
        }

        private void UpdateName()
        {
            FormatName = (LuminenceDepth != 0 ? "L" : "") +
                         (AlphaDepth != 0 ? "A" : "") +
                         (LuminenceDepth != 0 ? LuminenceDepth.ToString() : "") +
                         (AlphaDepth != 0 ? AlphaDepth.ToString() : "");
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            var lShift = AlphaDepth;

            var aBitMask = (1 << AlphaDepth) - 1;
            var lBitMask = (1 << LuminenceDepth) - 1;

            using (var br = new BinaryReader(new MemoryStream(tex)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value;

                    switch (BitDepth)
                    {
                        case 4:
                            value = br.ReadByte();

                            // high nibble first
                            yield return CreateColor((value & 0xF0) >> 4, aBitMask, lBitMask, lShift);
                            yield return CreateColor(value & 0xF, aBitMask, lBitMask, lShift);
                            break;
                        case 8:
                            value = br.ReadByte();
                            break;
                        case 16:
                            value = Kanvas.Support.Convert.FromByteArray<ushort>(br.ReadBytes(2), ByteOrder);
                            break;
                        default:
                            throw new InvalidOperationException($"BitDepth {BitDepth} not supported!");
                    }

                    yield return CreateColor(value, aBitMask, lBitMask, lShift);
                }
            }
        }

        private Color CreateColor(long value, int alphaBitMask, int lumBitMask, int lumShift)
        {
            return Color.FromArgb(
                (AlphaDepth == 0) ? 255 : Kanvas.Support.Convert.ChangeBitDepth((int)(value & alphaBitMask), AlphaDepth, 8),
                (LuminenceDepth == 0) ? 255 : Kanvas.Support.Convert.ChangeBitDepth((int)(value >> lumShift & lumBitMask), LuminenceDepth, 8),
                (LuminenceDepth == 0) ? 255 : Kanvas.Support.Convert.ChangeBitDepth((int)(value >> lumShift & lumBitMask), LuminenceDepth, 8),
                (LuminenceDepth == 0) ? 255 : Kanvas.Support.Convert.ChangeBitDepth((int)(value >> lumShift & lumBitMask), LuminenceDepth, 8));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            byte nibbleBuffer = 0;
            bool writeNibble = false;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            {
                foreach (var color in colors)
                {
                    var a = (AlphaDepth == 0) ? 0 : Kanvas.Support.Convert.ChangeBitDepth(color.A, 8, AlphaDepth);
                    var l = (LuminenceDepth == 0) ? 0 : Kanvas.Support.Convert.ChangeBitDepth(color.G, 8, LuminenceDepth);

                    var lShift = AlphaDepth;

                    long value = a;
                    value |= (uint)(l << lShift);

                    switch (BitDepth)
                    {
                        case 4:
                            if (writeNibble)
                            {
                                nibbleBuffer |= (byte)(value & 0xF);
                                bw.Write(nibbleBuffer);
                            }
                            else
                                nibbleBuffer = (byte)((value & 0xF) << 4);

                            writeNibble = !writeNibble;
                            break;
                        case 8:
                            bw.Write((byte)value);
                            break;
                        case 16:
                            bw.Write(Kanvas.Support.Convert.ToByteArray((ushort)value, 2, ByteOrder));
                            break;
                        default:
                            throw new InvalidOperationException($"BitDepth {BitDepth} not supported!");
                    }
                }

                if (writeNibble)
                    bw.Write(nibbleBuffer);
            }

            return ms.ToArray();
        }
    }
}
