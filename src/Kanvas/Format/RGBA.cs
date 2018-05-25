using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using Kanvas.Support;
using System.Drawing;
using Komponent.IO;
using System.IO;

namespace Kanvas.Format
{
    public class RGBA : IImageFormat
    {
        public int BitDepth { get; set; }

        public string FormatName { get; set; }

        int rDepth;
        int gDepth;
        int bDepth;
        int aDepth;

        bool alphaFirst;
        bool swapColorChannels;
        ByteOrder byteOrder;

        public RGBA(int r, int g, int b, int a = 0, bool alphaFirst = false, bool swapColorChannels = false, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = r + g + b + a;
            if (BitDepth < 8) throw new Exception($"Overall bitDepth can't be smaller than 8. Given bitDepth: {BitDepth}");
            if (BitDepth > 32) throw new Exception($"Overall bitDepth can't be bigger than 32. Given bitDepth: {BitDepth}");

            this.alphaFirst = alphaFirst;
            this.swapColorChannels = swapColorChannels;
            this.byteOrder = byteOrder;

            rDepth = r;
            gDepth = g;
            bDepth = b;
            aDepth = a;

            var tmpName = (alphaFirst && a > 0) ? "A" : "";
            tmpName += (swapColorChannels) ? (b > 0) ? "B" : "" : (r > 0) ? "R" : "";
            tmpName += (g > 0) ? "G" : "";
            tmpName += (swapColorChannels) ? (r > 0) ? "R" : "" : (b > 0) ? "B" : "";
            tmpName += (!alphaFirst && a > 0) ? "A" : "";

            tmpName += (alphaFirst && a > 0) ? a.ToString() : "";
            tmpName += (swapColorChannels) ? (b > 0) ? b.ToString() : "" : (r > 0) ? r.ToString() : "";
            tmpName += (g > 0) ? g.ToString() : "";
            tmpName += (swapColorChannels) ? (r > 0) ? r.ToString() : "" : (b > 0) ? b.ToString() : "";
            tmpName += (!alphaFirst && a > 0) ? a.ToString() : "";

            FormatName = tmpName;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                var (aShift, rShift, bShift, gShift) = GetBitShifts();
                var (aBitMask, rBitMask, gBitMask, bBitMask) = GetChannelMasks();

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value = 0;

                    if (BitDepth <= 8)
                        value = br.ReadByte();
                    else if (BitDepth <= 16)
                        value = br.ReadUInt16();
                    else if (BitDepth <= 24)
                    {
                        var tmp = br.ReadBytes(3);
                        value = (byteOrder == ByteOrder.LittleEndian) ? tmp[2] << 16 | tmp[1] << 8 | tmp[0] : tmp[0] << 16 | tmp[1] << 8 | tmp[0];
                    }
                    else if (BitDepth <= 32)
                        value = br.ReadUInt32();
                    else
                        throw new Exception($"BitDepth {BitDepth} not supported!");

                    yield return Color.FromArgb(
                        (aDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> aShift & aBitMask), aDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> rShift & rBitMask), rDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> gShift & gBitMask), gDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> bShift & bBitMask), bDepth, 8));
                }
            }
        }

        private (int, int, int, int) GetBitShifts()
        {
            int aShift = 0, rShift = 0, gShift = 0, bShift = 0;

            if (!alphaFirst)
            {
                if (swapColorChannels)
                {
                    rShift = aDepth;
                    gShift = rShift + rDepth;
                    bShift = gShift + gDepth;
                }
                else
                {
                    bShift = aDepth;
                    gShift = bShift + bDepth;
                    rShift = gShift + gDepth;
                }
            }
            else
            {
                if (swapColorChannels)
                {
                    gShift = rDepth;
                    bShift = gShift + gDepth;
                    aShift = bShift + bDepth;
                }
                else
                {
                    gShift = bDepth;
                    rShift = gShift + gDepth;
                    aShift = rShift + rDepth;
                }
            }

            return (aShift, rShift, gShift, bShift);
        }

        private (int, int, int, int) GetChannelMasks()
        {
            return ((1 << aDepth) - 1, (1 << rDepth) - 1, (1 << gDepth) - 1, (1 << bDepth) - 1);
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, byteOrder))
                foreach (var color in colors)
                {
                    var a = (aDepth == 0) ? 0 : Helper.ChangeBitDepth(color.A, 8, aDepth);
                    var r = Helper.ChangeBitDepth(color.R, 8, rDepth);
                    var g = Helper.ChangeBitDepth(color.G, 8, gDepth);
                    var b = Helper.ChangeBitDepth(color.B, 8, bDepth);

                    var (aShift, rShift, gShift, bShift) = GetBitShifts();

                    long value = 0;
                    value |= (uint)(a << aShift);
                    value |= (uint)(b << bShift);
                    value |= (uint)(g << gShift);
                    value |= (uint)(r << rShift);

                    if (BitDepth <= 8)
                        bw.Write((byte)value);
                    else if (BitDepth <= 16)
                        bw.Write((ushort)value);
                    else if (BitDepth <= 24)
                    {
                        var tmp = (byteOrder == ByteOrder.LittleEndian) ?
                                new byte[] { (byte)(value & 0xff), (byte)(value >> 8 & 0xff), (byte)(value >> 16 & 0xff) } :
                                new byte[] { (byte)(value >> 16 & 0xff), (byte)(value >> 8 & 0xff), (byte)(value & 0xff) };
                        bw.Write(tmp);
                    }
                    else if (BitDepth <= 32)
                        bw.Write((uint)value);
                    else
                        throw new Exception($"BitDepth {BitDepth} not supported!");
                }

            return ms.ToArray();
        }
    }
}
