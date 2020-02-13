using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelEncoding : IColorEncoding
    {
        private readonly IPixelDescriptor _descriptor;
        private readonly ByteOrder _byteOrder;

        private Func<BinaryReaderX, long> _readValueDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        public int BitDepth { get; }

        public int BlockBitDepth { get; }

        public bool IsBlockCompression => false;

        public string FormatName { get; }

        protected PixelEncoding(IPixelDescriptor pixelDescriptor, ByteOrder byteOrder)
        {
            _descriptor = pixelDescriptor;

            BitDepth = BlockBitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();

            SetValueDelegates(BitDepth);
        }

        public IEnumerable<Color> Load(byte[] input)
        {
            using var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
                yield return _descriptor.GetColor(_readValueDelegate(br));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            foreach (var color in colors)
                _writeValueDelegate(bw, _descriptor.GetValue(color));

            return ms.ToArray();
        }

        private void SetValueDelegates(int bitDepth)
        {
            var bytesToRead = bitDepth / 8 + (bitDepth % 8 > 0 ? 1 : 0);

            if (bitDepth == 4)
            {
                _readValueDelegate = br => br.ReadNibble();
                _writeValueDelegate = (bw, value) => bw.WriteNibble((int)value);
                return;
            }

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
