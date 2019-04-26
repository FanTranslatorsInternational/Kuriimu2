using Kanvas.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas.Format.Support;
using Kanvas.Models;

namespace Kanvas.Format
{
    /// <summary>
    /// Defines the RGBA encoding.
    /// </summary>
    public class RGBA : IColorTranscoding
    {
        private bool _isAlphaFirst;
        private bool _shouldSwapColorChannels;

        /// <inheritdoc cref="IColorTranscoding.BitDepth"/>
        public int BitDepth { get; set; }

        /// <inheritdoc cref="IColorTranscoding.IsBlockCompression"/>
        public bool IsBlockCompression => false;

        /// <inheritdoc cref="IColorTranscoding.FormatName"/>
        public string FormatName { get; set; }

        /// <summary>
        /// The bit depth of the red component.
        /// </summary>
        public int RedDepth { get; }

        /// <summary>
        /// The bit depth of the green component.
        /// </summary>
        public int GreenDepth { get; }

        /// <summary>
        /// The bit depth of the blue component.
        /// </summary>
        public int BlueDepth { get; }

        /// <summary>
        /// The bit depth of the alpha component.
        /// </summary>
        public int AlphaDepth { get; }

        /// <summary>
        /// Should the alpha component be interpreted before the color components.
        /// </summary>
        public bool IsAlphaFirst
        {
            get => _isAlphaFirst;
            set
            {
                _isAlphaFirst = value;
                UpdateName();
            }
        }

        /// <summary>
        /// Should the color components be interpreted in reverse.
        /// </summary>
        /// <remarks>If <c>false</c>, RGB is used.</remarks>
        /// <remarks>If <c>true</c>, BGR is used.</remarks>
        public bool ShouldSwapColorChannels
        {
            get => _shouldSwapColorChannels;
            set
            {
                _shouldSwapColorChannels = value;
                UpdateName();
            }
        }

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        public RGBA(int r, int g, int b) : this(r, g, b, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        public RGBA(int r, int g, int b, int a)
        {
            BitDepth = r + g + b + a;
            if (BitDepth < 8) throw new InvalidOperationException($"Summed bit depth can't be smaller than 8. Given bit depth: {BitDepth}");
            if (BitDepth > 32) throw new InvalidOperationException($"Summed bit depth can't be bigger than 32. Given bit depth: {BitDepth}");

            RedDepth = r;
            GreenDepth = g;
            BlueDepth = b;
            AlphaDepth = a;

            UpdateName();
        }

        private void UpdateName()
        {
            var tmpName = (_isAlphaFirst && AlphaDepth > 0) ? "A" : "";
            tmpName += (_shouldSwapColorChannels) ? (BlueDepth > 0) ? "B" : "" : (RedDepth > 0) ? "R" : "";
            tmpName += (GreenDepth > 0) ? "G" : "";
            tmpName += (_shouldSwapColorChannels) ? (RedDepth > 0) ? "R" : "" : (BlueDepth > 0) ? "B" : "";
            tmpName += (!_isAlphaFirst && AlphaDepth > 0) ? "A" : "";

            tmpName += (_isAlphaFirst && AlphaDepth > 0) ? AlphaDepth.ToString() : "";
            tmpName += (_shouldSwapColorChannels) ? (BlueDepth > 0) ? BlueDepth.ToString() : "" : (RedDepth > 0) ? RedDepth.ToString() : "";
            tmpName += (GreenDepth > 0) ? GreenDepth.ToString() : "";
            tmpName += (_shouldSwapColorChannels) ? (RedDepth > 0) ? RedDepth.ToString() : "" : (BlueDepth > 0) ? BlueDepth.ToString() : "";
            tmpName += (!_isAlphaFirst && AlphaDepth > 0) ? AlphaDepth.ToString() : "";

            FormatName = tmpName;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReader(new MemoryStream(tex)))
            {
                var (aShift, rShift, gShift, bShift) = GetBitShifts();
                var (aBitMask, rBitMask, gBitMask, bBitMask) = GetChannelMasks();

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value;

                    if (BitDepth <= 8)
                        value = br.ReadByte();
                    else if (BitDepth <= 16)
                        value = Convert.FromByteArray<ushort>(br.ReadBytes(2), ByteOrder);
                    else if (BitDepth <= 24)
                        value = Convert.FromByteArray<uint>(br.ReadBytes(3), ByteOrder);
                    else if (BitDepth <= 32)
                        value = Convert.FromByteArray<uint>(br.ReadBytes(4), ByteOrder);
                    else
                        throw new InvalidOperationException($"BitDepth {BitDepth} not supported!");

                    yield return Color.FromArgb(
                        (AlphaDepth == 0) ? 255 : Helper.ChangeBitDepth((int)(value >> aShift & aBitMask), AlphaDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> rShift & rBitMask), RedDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> gShift & gBitMask), GreenDepth, 8),
                        Helper.ChangeBitDepth((int)(value >> bShift & bBitMask), BlueDepth, 8));
                }
            }
        }

        private (int, int, int, int) GetBitShifts()
        {
            int aShift = 0, rShift = 0, gShift, bShift = 0;

            if (!_isAlphaFirst)
            {
                if (_shouldSwapColorChannels)
                {
                    rShift = AlphaDepth;
                    gShift = rShift + RedDepth;
                    bShift = gShift + GreenDepth;
                }
                else
                {
                    bShift = AlphaDepth;
                    gShift = bShift + BlueDepth;
                    rShift = gShift + GreenDepth;
                }
            }
            else
            {
                if (_shouldSwapColorChannels)
                {
                    gShift = RedDepth;
                    bShift = gShift + GreenDepth;
                    aShift = bShift + BlueDepth;
                }
                else
                {
                    gShift = BlueDepth;
                    rShift = gShift + GreenDepth;
                    aShift = rShift + RedDepth;
                }
            }

            return (aShift, rShift, gShift, bShift);
        }

        private (int, int, int, int) GetChannelMasks()
        {
            return ((1 << AlphaDepth) - 1, (1 << RedDepth) - 1, (1 << GreenDepth) - 1, (1 << BlueDepth) - 1);
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
                foreach (var color in colors)
                {
                    var a = (AlphaDepth == 0) ? 0 : Helper.ChangeBitDepth(color.A, 8, AlphaDepth);
                    var r = Helper.ChangeBitDepth(color.R, 8, RedDepth);
                    var g = Helper.ChangeBitDepth(color.G, 8, GreenDepth);
                    var b = Helper.ChangeBitDepth(color.B, 8, BlueDepth);

                    var (aShift, rShift, gShift, bShift) = GetBitShifts();

                    long value = 0;
                    value |= (uint)(a << aShift);
                    value |= (uint)(b << bShift);
                    value |= (uint)(g << gShift);
                    value |= (uint)(r << rShift);

                    if (BitDepth <= 8)
                        bw.Write((byte)value);
                    else if (BitDepth <= 16)
                        bw.Write(Support.Convert.ToByteArray((ushort)value, 2, ByteOrder));
                    else if (BitDepth <= 24)
                        bw.Write(Support.Convert.ToByteArray((uint)value, 3, ByteOrder));
                    else if (BitDepth <= 32)
                        bw.Write(Support.Convert.ToByteArray((uint)value, 4, ByteOrder));
                    else
                        throw new Exception($"BitDepth {BitDepth} not supported!");
                }

            return ms.ToArray();
        }
    }
}
