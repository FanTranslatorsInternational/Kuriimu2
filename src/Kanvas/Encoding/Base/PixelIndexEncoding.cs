using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelIndexEncoding : IIndexEncoding
    {
        private readonly IPixelIndexDescriptor _descriptor;
        private readonly ByteOrder _byteOrder;
        private readonly BitOrder _bitOrder;

        private Func<BinaryReaderX, IList<long>> _readValuesDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        public int BitDepth { get; }

        public int BitsPerValue { get; private set; }

        public int ColorsPerValue { get; }

        public string FormatName { get; }

        public int MaxColors { get; protected set; }

        protected PixelIndexEncoding(IPixelIndexDescriptor pixelDescriptor, ByteOrder byteOrder, BitOrder bitOrder)
        {
            _descriptor = pixelDescriptor;
            _byteOrder = byteOrder;
            _bitOrder = bitOrder;

            BitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();
            ColorsPerValue = 1;

            SetValueDelegates(BitDepth);
        }

        public IEnumerable<Color> Load(byte[] input, IList<Color> palette, int taskCount)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder, NibbleOrder.LowNibbleFirst, _bitOrder, 1);

            return ReadValues(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(c => _descriptor.GetColor(c, palette));
        }

        public byte[] Save(IEnumerable<int> indices, IList<Color> palette, int taskCount)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder, NibbleOrder.LowNibbleFirst, _bitOrder, 1);

            var values = indices.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(i => _descriptor.GetValue(i, palette));

            foreach (var value in values)
                _writeValueDelegate(bw, value);

            return ms.ToArray();
        }

        private IEnumerable<long> ReadValues(BinaryReaderX br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
                foreach (var value in _readValuesDelegate(br))
                    yield return value;
        }

        private void SetValueDelegates(int bitDepth)
        {
            IList<long> ReadBitValues(BinaryReaderX br, int bitLength)
            {
                var valueCount = (br.BlockSize * 8 + (bitLength - 1)) / bitLength;
                var result = new long[valueCount];

                for (var i = 0; i < valueCount; i++)
                    result[i] = br.ReadBits<long>(bitLength);

                return result;
            }

            BitsPerValue = bitDepth;
            switch (bitDepth)
            {
                case 1:
                    _readValuesDelegate = br => ReadBitValues(br, bitDepth);
                    _writeValueDelegate = (bw, value) => bw.WriteBits(value, 1);
                    break;

                case 2:
                    _readValuesDelegate = br => ReadBitValues(br, bitDepth);
                    _writeValueDelegate = (bw, value) => bw.WriteBits(value, 2);
                    break;

                case 4:
                    _readValuesDelegate = br => ReadBitValues(br, bitDepth);
                    _writeValueDelegate = (bw, value) => bw.WriteBits(value, 4);
                    break;

                case 8:
                    _readValuesDelegate = br => new long[] { br.ReadByte() };
                    _writeValueDelegate = (bw, value) => bw.Write((byte)value);
                    break;

                case 16:
                    _readValuesDelegate = br => new long[] { br.ReadUInt16() };
                    _writeValueDelegate = (bw, value) => bw.Write((ushort)value);
                    break;
            }
        }
    }
}
