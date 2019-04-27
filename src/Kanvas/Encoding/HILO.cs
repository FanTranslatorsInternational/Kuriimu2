using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Interface;
using Kanvas.Models;
using Convert = Kanvas.Support.Convert;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the HILO encoding.
    /// </summary>
    public class HILO : IColorEncoding
    {
        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => false;

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        /// <summary>
        /// The bit depth of the red component.
        /// </summary>
        public int RedDepth { get; }

        /// <summary>
        /// The bit depth of the green component.
        /// </summary>
        public int GreenDepth { get; }

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        public HILO(int r, int g)
        {
            BitDepth = r + g;
            if (BitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (BitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (BitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (r < 4 && g < 4) throw new Exception($"Red and Green value can't be smaller than 4.\nGiven Red: {r}; Given Green: {g}");

            RedDepth = r;
            GreenDepth = g;

            FormatName = "HILO" + r + g;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReader(new MemoryStream(tex)))
            {
                var rShift = GreenDepth;

                var gBitMask = (1 << GreenDepth) - 1;
                var rBitMask = (1 << RedDepth) - 1;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value;

                    switch (BitDepth)
                    {
                        case 4:
                            value = br.ReadByte();

                            // high nibble first
                            yield return CreateColor((value & 0xF0) >> 4, rBitMask, gBitMask, rShift);
                            yield return CreateColor(value & 0xF, rBitMask, gBitMask, rShift);
                            break;
                        case 8:
                            value = br.ReadByte();
                            break;
                        case 16:
                            value = Convert.FromByteArray<ushort>(br.ReadBytes(2), ByteOrder);
                            break;
                        default:
                            throw new InvalidOperationException($"BitDepth {BitDepth} not supported!");
                    }

                    yield return CreateColor(value, rBitMask, gBitMask, rShift);
                }
            }
        }

        private Color CreateColor(long value, int redBitMask, int greenBitMask, int redShift)
        {
            return Color.FromArgb(
                255,
                (RedDepth == 0) ? 255 : Convert.ChangeBitDepth((int)(value >> redShift & redBitMask), RedDepth, 8),
                (GreenDepth == 0) ? 255 : Convert.ChangeBitDepth((int)(value & greenBitMask), GreenDepth, 8),
                255);
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
                    var r = (RedDepth == 0) ? 0 : Convert.ChangeBitDepth(color.R, 8, RedDepth);
                    var g = (GreenDepth == 0) ? 0 : Convert.ChangeBitDepth(color.G, 8, GreenDepth);

                    var rShift = GreenDepth;

                    long value = g;
                    value |= (uint)(r << rShift);

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
                            bw.Write(Convert.ToByteArray((ushort)value, 2, ByteOrder));
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
