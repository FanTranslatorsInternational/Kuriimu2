using System.Buffers.Binary;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Contract.Encoding.Descriptor;
using Komponent.Contract.Enums;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelEncoding : IColorEncoding
    {
        private readonly IPixelDescriptor _descriptor;
        private readonly Func<byte[], int, IEnumerable<long>>? _readValuesDelegate;
        private readonly Action<IEnumerable<long>, byte[]>? _writeValuesDelegate;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; private set; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue { get; }

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        protected PixelEncoding(IPixelDescriptor pixelDescriptor, ByteOrder byteOrder, BitOrder bitOrder)
        {
            _descriptor = pixelDescriptor;

            BitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();
            ColorsPerValue = 1;

            _readValuesDelegate = GetReadDelegate(BitDepth, byteOrder, bitOrder);
            _writeValuesDelegate = GetWriteDelegate(BitDepth, byteOrder, bitOrder);
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions options)
        {
            if (_readValuesDelegate is null)
                return [];

            var bits = options.Size.Width * options.Size.Height * BitsPerValue;
            var length = bits / 8 + (bits % 8 > 0 ? 1 : 0);

            return _readValuesDelegate(input, length).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(options.TaskCount)
                .Select(_descriptor.GetColor);
        }

        /// <inheritdoc cref="Load"/>
        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            if (_writeValuesDelegate is null)
                return [];

            var values = colors.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(options.TaskCount)
                .Select(_descriptor.GetValue);

            var bits = options.Size.Width * options.Size.Height * BitsPerValue;
            var buffer = new byte[bits / 8 + (bits % 8 > 0 ? 1 : 0)];
            _writeValuesDelegate(values, buffer);

            return buffer;
        }

        #region Delegate getter

        private Func<byte[], int, IEnumerable<long>>? GetReadDelegate(int bitDepth, ByteOrder byteOrder, BitOrder bitOrder)
        {
            BitsPerValue = bitDepth;

            if (bitDepth is 1 or 2 or 4)
            {
                if (bitOrder == BitOrder.MostSignificantBitFirst)
                    return (input, length) => ReadBitsMSB(input, length, bitDepth);

                return (input, length) => ReadBitsLSB(input, length, bitDepth);
            }

            if (bitDepth < 8)
                return null;

            var bytesToRead = (bitDepth + 7) >> 3;
            BitsPerValue = bytesToRead * 8;

            switch (bytesToRead)
            {
                case 1:
                    return ReadBitDepth8;

                case 2:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return ReadBitDepth16LE;

                    return ReadBitDepth16BE;

                case 3:
                    return ReadBitDepth24;

                case 4:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return ReadBitDepth32LE;

                    return ReadBitDepth32BE;
            }

            return null;
        }

        private Action<IEnumerable<long>, byte[]>? GetWriteDelegate(int bitDepth, ByteOrder byteOrder, BitOrder bitOrder)
        {
            if (bitDepth is 1 or 2 or 4)
            {
                if (bitOrder == BitOrder.MostSignificantBitFirst)
                    return (values, input) => WriteBitsMSB(values, input, bitDepth);

                return (values, input) => WriteBitsLSB(values, input, bitDepth);
            }

            if (bitDepth < 8)
                return null;

            var bytesToRead = (bitDepth + 7) >> 3;
            switch (bytesToRead)
            {
                case 1:
                    return WriteBitDepth8;

                case 2:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return WriteBitDepth16LE;

                    return WriteBitDepth16BE;

                case 3:
                    return WriteBitDepth24;

                case 4:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return WriteBitDepth32LE;

                    return WriteBitDepth32BE;
            }

            return null;
        }

        #endregion

        #region Read delegates

        private IEnumerable<long> ReadBitsMSB(byte[] input, int length, int bitDepth)
        {
            int valueCount = 8 / bitDepth;
            int mask = (1 << bitDepth) - 1;

            for (var i = 0; i < length; i++)
            {
                for (var j = valueCount - 1; j >= 0; j--)
                    yield return (input[i] >> (bitDepth * j)) & mask;
            }
        }
        private IEnumerable<long> ReadBitsLSB(byte[] input, int length, int bitDepth)
        {
            int valueCount = 8 / bitDepth;
            int mask = (1 << bitDepth) - 1;

            for (var i = 0; i < length; i++)
            {
                for (var j = 0; j < valueCount; j++)
                    yield return (input[i] >> (bitDepth * j)) & mask;
            }
        }

        private IEnumerable<long> ReadBitDepth8(byte[] input, int length)
        {
            for (var i = 0; i < length; i++)
                yield return input[i];
        }

        private IEnumerable<long> ReadBitDepth16LE(byte[] input, int length)
        {
            for (var i = 0; i < length; i += 2)
                yield return BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(i, 2));
        }
        private IEnumerable<long> ReadBitDepth16BE(byte[] input, int length)
        {
            for (var i = 0; i < length; i += 2)
                yield return BinaryPrimitives.ReadUInt16BigEndian(input.AsSpan(i, 2));
        }

        private IEnumerable<long> ReadBitDepth24(byte[] input, int length)
        {
            for (var i = 0; i < length; i += 3)
                yield return (input[i] << 16) | (input[i + 1] << 8) | input[i + 2];
        }

        private IEnumerable<long> ReadBitDepth32LE(byte[] input, int length)
        {
            for (var i = 0; i < length; i += 4)
                yield return BinaryPrimitives.ReadUInt32LittleEndian(input.AsSpan(i, 4));
        }
        private IEnumerable<long> ReadBitDepth32BE(byte[] input, int length)
        {
            for (var i = 0; i < length; i += 4)
                yield return BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(i, 4));
        }

        #endregion

        #region Write delegates

        private void WriteBitsMSB(IEnumerable<long> values, byte[] input, int bitDepth)
        {
            var index = 0;
            var shift = 7;

            foreach (var value in values.Take(input.Length * 8))
            {
                input[index] |= (byte)(value << shift);
                shift -= bitDepth;

                if (shift >= 0)
                    continue;

                index++;
                shift = 7;
            }
        }
        private void WriteBitsLSB(IEnumerable<long> values, byte[] input, int bitDepth)
        {
            var index = 0;
            var shift = 0;

            foreach (var value in values.Take(input.Length * 8))
            {
                input[index] |= (byte)(value << shift);
                shift += bitDepth;

                if (shift >= 7)
                    continue;

                index++;
                shift = 0;
            }
        }

        private void WriteBitDepth8(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length))
                input[index++] = (byte)value;
        }

        private void WriteBitDepth16LE(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length / 2))
            {
                BinaryPrimitives.WriteUInt16LittleEndian(input.AsSpan(index, 2), (ushort)value);
                index += 2;
            }
        }
        private void WriteBitDepth16BE(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length / 2))
            {
                BinaryPrimitives.WriteUInt16BigEndian(input.AsSpan(index, 2), (ushort)value);
                index += 2;
            }
        }

        private void WriteBitDepth24(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length / 3))
            {
                input[index++] = (byte)(value >> 16);
                input[index++] = (byte)(value >> 8);
                input[index++] = (byte)(value);
            }
        }

        private void WriteBitDepth32LE(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length / 4))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(input.AsSpan(index, 4), (uint)value);
                index += 4;
            }
        }
        private void WriteBitDepth32BE(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            foreach (var value in values.Take(input.Length / 4))
            {
                BinaryPrimitives.WriteUInt32BigEndian(input.AsSpan(index, 4), (uint)value);
                index += 4;
            }
        }

        #endregion
    }
}
