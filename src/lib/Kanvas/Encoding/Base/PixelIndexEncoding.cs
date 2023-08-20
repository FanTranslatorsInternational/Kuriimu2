using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;
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

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; private set; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue { get; }

        /// <inheritdoc cref="FormatName"/>
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

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Color> Load(byte[] input, IList<Color> palette, EncodingLoadContext loadContext)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder, _bitOrder, 1);

            return ReadValues(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .Select(i => _descriptor.GetColor(i, palette));
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<int> indices, IList<Color> palette, EncodingSaveContext saveContext)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder, _bitOrder, 1);

            var values = indices.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(saveContext.TaskCount)
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
