using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.Models;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the RGBA encoding.
    /// </summary>
    public class RGBA : IColorEncoding
    {
        private readonly ByteOrder _byteOrder;

        private readonly RgbaPixelDescriptor _descriptor;
        private Func<BinaryReaderX, long> _readValueDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        /// <inheritdoc />
        public int BitDepth { get; }

        /// <inheritdoc />
        public bool IsBlockCompression => false;

        /// <inheritdoc />
        public string FormatName { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            this(r, g, b, 0, componentOrder, byteOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, int a, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            _descriptor = new RgbaPixelDescriptor(componentOrder, r, g, b, a);
            _byteOrder = byteOrder;

            var bitDepth = r + g + b + a;
            var bytesToRead = bitDepth / 8 + (bitDepth % 8 > 0 ? 1 : 0);
            SetValueDelegates(bytesToRead);

            BitDepth = bitDepth;
            FormatName = _descriptor.GetPixelName();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using var br = new BinaryReaderX(new MemoryStream(tex), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var value = _readValueDelegate(br);
                yield return _descriptor.GetColor(value);
            }

            //using (var br = new BinaryReader(new MemoryStream(tex)))
            //{
            //    var (aShift, rShift, gShift, bShift) = GetBitShifts();
            //    var (aBitMask, rBitMask, gBitMask, bBitMask) = GetChannelMasks();

            //    while (br.BaseStream.Position < br.BaseStream.Length)
            //    {
            //        long value;

            //        if (BitDepth <= 8)
            //            value = br.ReadByte();
            //        else if (BitDepth <= 16)
            //            value = Kanvas.Support.Convert.FromByteArray<ushort>(br.ReadBytes(2), ByteOrder);
            //        else if (BitDepth <= 24)
            //            value = Kanvas.Support.Convert.FromByteArray<uint>(br.ReadBytes(3), ByteOrder);
            //        else if (BitDepth <= 32)
            //            value = Kanvas.Support.Convert.FromByteArray<uint>(br.ReadBytes(4), ByteOrder);
            //        else
            //            throw new InvalidOperationException($"BitDepth {BitDepth} not supported!");

            //        yield return Color.FromArgb(
            //            (AlphaDepth == 0) ? 255 : Kanvas.Support.Convert.ChangeBitDepth((int)(value >> aShift & aBitMask), AlphaDepth, 8),
            //            Kanvas.Support.Convert.ChangeBitDepth((int)(value >> rShift & rBitMask), RedDepth, 8),
            //            Kanvas.Support.Convert.ChangeBitDepth((int)(value >> gShift & gBitMask), GreenDepth, 8),
            //            Kanvas.Support.Convert.ChangeBitDepth((int)(value >> bShift & bBitMask), BlueDepth, 8));
            //    }
            //}
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            foreach (var color in colors)
                _writeValueDelegate(bw, _descriptor.GetValue(color));

            return ms.ToArray();

            //while (bw.BaseStream.Position < br.BaseStream.Length)
            //{
            //    var value = _readValueDelegate(br);
            //    yield return _descriptor.GetColor(value);
            //}

            //using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            //    foreach (var color in colors)
            //    {
            //        var a = (AlphaDepth == 0) ? 0 : Kanvas.Support.Convert.ChangeBitDepth(color.A, 8, AlphaDepth);
            //        var r = Kanvas.Support.Convert.ChangeBitDepth(color.R, 8, RedDepth);
            //        var g = Kanvas.Support.Convert.ChangeBitDepth(color.G, 8, GreenDepth);
            //        var b = Kanvas.Support.Convert.ChangeBitDepth(color.B, 8, BlueDepth);

            //        var (aShift, rShift, gShift, bShift) = GetBitShifts();

            //        long value = 0;
            //        value |= (uint)(a << aShift);
            //        value |= (uint)(b << bShift);
            //        value |= (uint)(g << gShift);
            //        value |= (uint)(r << rShift);

            //        if (BitDepth <= 8)
            //            bw.Write((byte)value);
            //        else if (BitDepth <= 16)
            //            bw.Write(Kanvas.Support.Convert.ToByteArray((ushort)value, 2, ByteOrder));
            //        else if (BitDepth <= 24)
            //            bw.Write(Kanvas.Support.Convert.ToByteArray((uint)value, 3, ByteOrder));
            //        else if (BitDepth <= 32)
            //            bw.Write(Kanvas.Support.Convert.ToByteArray((uint)value, 4, ByteOrder));
            //        else
            //            throw new Exception($"BitDepth {BitDepth} not supported!");
            //    }
        }

        private void SetValueDelegates(int bytesToRead)
        {
            switch (bytesToRead)
            {
                case 1:
                    _readValueDelegate = br => br.ReadByte();
                    _writeValueDelegate = (bw, value) => bw.Write((byte)value);
                    break;

                case 2:
                    _readValueDelegate = br => br.ReadUInt16();
                    _writeValueDelegate = (bw, value) => bw.Write((ushort)value);
                    break;

                case 3:
                    _readValueDelegate = br =>
                    {
                        var bytes = br.ReadBytes(3);
                        return (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
                    };
                    _writeValueDelegate = (bw, value) =>
                    {
                        var bytes = new[] { (byte)(value >> 16), (byte)(value >> 8), (byte)value };
                        bw.Write(bytes);
                    };
                    break;

                case 4:
                    _readValueDelegate = br => br.ReadUInt32();
                    _writeValueDelegate = (bw, value) => bw.Write((uint)value);
                    break;
            }
        }
    }
}
