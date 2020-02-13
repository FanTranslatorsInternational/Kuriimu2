using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelIndexEncoding : IColorIndexEncoding
    {
        private readonly IPixelIndexDescriptor _indexDescriptor;
        private readonly ByteOrder _byteOrder;

        private Func<BinaryReaderX, long> _readValueDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        public int BitDepth { get; }

        public bool IsBlockCompression => false;

        public string FormatName { get; }

        protected PixelIndexEncoding(IPixelIndexDescriptor pixelIndexDescriptor, ByteOrder byteOrder)
        {
            _indexDescriptor = pixelIndexDescriptor;

            BitDepth = pixelIndexDescriptor.GetBitDepth();
            FormatName = pixelIndexDescriptor.GetPixelName();

            SetValueDelegates(BitDepth);
        }

        public IEnumerable<Color> Load(byte[] input, IList<Color> palette)
        {
            using var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
                yield return _indexDescriptor.GetColor(_readValueDelegate(br), palette);
        }

        public byte[] Save(IEnumerable<int> indices, IList<Color> palette)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            foreach (var index in indices)
                _writeValueDelegate(bw, _indexDescriptor.GetValue(index, palette));

            return ms.ToArray();
        }

        private void SetValueDelegates(int bitDepth)
        {
            switch (bitDepth)
            {
                case 1:
                    _readValueDelegate = br => br.ReadBits<long>(1);
                    _writeValueDelegate = (bw, value) => bw.WriteBits(value, 1);
                    break;

                case 2:
                    _readValueDelegate = br => br.ReadBits<long>(2);
                    _writeValueDelegate = (bw, value) => bw.WriteBits(value, 2);
                    break;

                case 4:
                    _readValueDelegate = br => br.ReadNibble();
                    _writeValueDelegate = (bw, value) => bw.WriteNibble((int)value);
                    break;

                case 8:
                    _readValueDelegate = br => br.ReadByte();
                    _writeValueDelegate = (bw, value) => bw.Write((byte)value);
                    break;

                case 16:
                    _readValueDelegate = br => br.ReadUInt16();
                    _writeValueDelegate = (bw, value) => bw.Write((ushort)value);
                    break;
            }
        }
    }
}
