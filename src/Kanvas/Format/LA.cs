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
    public class LA : IImageFormat
    {
        public int BitDepth { get; set; }

        public string FormatName { get; set; }

        int lDepth;
        int aDepth;

        ByteOrder byteOrder;

        public LA(int l, int a, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = a + l;
            if (BitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (BitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (BitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (l < 4 && a < 4) throw new Exception($"Luminance and Alpha value can't be smaller than 4.\nGiven Luminance: {l}; Given Alpha: {a}");

            lDepth = l;
            aDepth = a;

            FormatName = ((l != 0) ? "L" : "") + ((a != 0) ? "A" : "") + ((l != 0) ? l.ToString() : "") + ((a != 0) ? a.ToString() : "");

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                var lShift = aDepth;

                var aBitMask = (1 << aDepth) - 1;
                var lBitMask = (1 << lDepth) - 1;

                while (true)
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
                        (aDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value & aBitMask), aDepth, 8),
                        (lDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> lShift & lBitMask), lDepth, 8),
                        (lDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> lShift & lBitMask), lDepth, 8),
                        (lDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> lShift & lBitMask), lDepth, 8));
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, byteOrder))
                foreach (var color in colors)
                {
                    var a = (aDepth == 0) ? 0 : Helper.ChangeBitDepth(color.A, 8, aDepth);
                    var l = (lDepth == 0) ? 0 : Helper.ChangeBitDepth(color.G, 8, lDepth);

                    var lShift = aDepth;

                    long value = a;
                    value |= (uint)(l << lShift);

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
