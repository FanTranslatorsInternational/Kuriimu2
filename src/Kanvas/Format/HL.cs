using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;
using Komponent.IO;
using System.IO;
using Kanvas.Support;

namespace Kanvas.Format
{
    public class HL : IImageFormat
    {
        public int BitDepth { get; set; }

        public bool IsBlockCompression { get => false; }

        public string FormatName { get; set; }

        int rDepth;
        int gDepth;

        ByteOrder byteOrder;

        public HL(int r, int g, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = r + g;
            if (BitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (BitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (BitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (r < 4 && g < 4) throw new Exception($"Red and Green value can't be smaller than 4.\nGiven Red: {r}; Given Green: {g}");

            rDepth = r;
            gDepth = g;

            FormatName = "HL" + r.ToString() + g.ToString();

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                var rShift = gDepth;

                var gBitMask = (1 << gDepth) - 1;
                var rBitMask = (1 << rDepth) - 1;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value = 0;

                    switch (BitDepth)
                    {
                        case 4:
                            value = br.ReadNibble();
                            break;
                        case 8:
                            value = br.ReadByte();
                            break;
                        case 16:
                            value = br.ReadUInt16();
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }

                    yield return Color.FromArgb(
                        255,
                        (rDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> rShift & rBitMask), rDepth, 8),
                        (gDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value & gBitMask), gDepth, 8),
                        255);
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, byteOrder))
                foreach (var color in colors)
                {
                    var r = (rDepth == 0) ? 0 : Helper.ChangeBitDepth(color.R, 8, rDepth);
                    var g = (gDepth == 0) ? 0 : Helper.ChangeBitDepth(color.G, 8, gDepth);

                    var rShift = gDepth;

                    long value = g;
                    value |= (uint)(r << rShift);

                    switch (BitDepth)
                    {
                        case 4:
                            bw.WriteNibble((int)value);
                            break;
                        case 8:
                            bw.Write((byte)value);
                            break;
                        case 16:
                            bw.Write((ushort)value);
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }
                }

            return ms.ToArray();
        }
    }
}
