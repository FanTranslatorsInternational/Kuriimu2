using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelEncoding : IColorEncoding
    {
        private readonly IPixelDescriptor _descriptor;
        private readonly Func<byte[], int, IEnumerable<long>> _readValuesDelegate;
        private readonly Action<IEnumerable<long>, byte[]> _writeValuesDelegate;

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
            ContractAssertions.IsNotNull(pixelDescriptor, nameof(pixelDescriptor));

            _descriptor = pixelDescriptor;

            BitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();
            ColorsPerValue = 1;

            _readValuesDelegate = GetReadDelegate(BitDepth, byteOrder, bitOrder);
            _writeValuesDelegate = GetWriteDelegate(BitDepth, byteOrder, bitOrder);
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext)
        {
            var bits = loadContext.Size.Width * loadContext.Size.Height * BitsPerValue;
            var length = bits / 8 + (bits % 8 > 0 ? 1 : 0);

            return _readValuesDelegate(input, length).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .Select(v => _descriptor.GetColor(v));
        }

        /// <inheritdoc cref="Load"/>
        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            var values = colors.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(saveContext.TaskCount)
                .Select(c => _descriptor.GetValue(c));

            var bits = saveContext.Size.Width * saveContext.Size.Height * BitsPerValue;
            var buffer = new byte[bits / 8 + (bits % 8 > 0 ? 1 : 0)];
            _writeValuesDelegate(values, buffer);

            return buffer;
        }

        #region Delegate getter

        private Func<byte[], int, IEnumerable<long>> GetReadDelegate(int bitDepth, ByteOrder byteOrder, BitOrder bitOrder)
        {
            if (bitDepth == 4)
            {
                BitsPerValue = 4;

                if (bitOrder == BitOrder.MostSignificantBitFirst)
                    return ReadBitDepth4MSB;
                else
                    return ReadBitDepth4LSB;
            }

            var bytesToRead = (bitDepth + 7) >> 3;
            BitsPerValue = bytesToRead * 8;

            switch (bytesToRead)
            {
                case 1:
                    return ReadBitDepth8;

                case 2:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return ReadBitDepth16LE;
                    else
                        return ReadBitDepth16BE;

                case 3:
                    return ReadBitDepth24;

                case 4:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return ReadBitDepth32LE;
                    else
                        return ReadBitDepth32BE;
            }

            return null;
        }

        private Action<IEnumerable<long>, byte[]> GetWriteDelegate(int bitDepth, ByteOrder byteOrder, BitOrder bitOrder)
        {
            if (bitDepth == 4)
            {
                if (bitOrder == BitOrder.MostSignificantBitFirst)
                    return WriteBitDepth4MSB;
                else
                    return WriteBitDepth4LSB;
            }

            var bytesToRead = (bitDepth + 7) >> 3;
            switch (bytesToRead)
            {
                case 1:
                    return WriteBitDepth8;

                case 2:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return WriteBitDepth16LE;
                    else
                        return WriteBitDepth16BE;

                case 3:
                    return WriteBitDepth24;

                case 4:
                    if (byteOrder == ByteOrder.LittleEndian)
                        return WriteBitDepth32LE;
                    else
                        return WriteBitDepth32BE;
            }

            return null;
        }

        #endregion

        #region Read delegates

        private IEnumerable<long> ReadBitDepth4MSB(byte[] input, int length)
        {
            for (var i = 0; i < length; i++)
            {
                yield return input[i] >> 4;
                yield return input[i] & 15;
            }
        }
        private IEnumerable<long> ReadBitDepth4LSB(byte[] input, int length)
        {
            for (var i = 0; i < length; i++)
            {
                yield return input[i] & 15;
                yield return input[i] >> 4;
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

        private void WriteBitDepth4MSB(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            var shift = 4;

            foreach (var value in values.Take(input.Length * 2))
            {
                input[index] |= (byte)(value << shift);
                shift ^= 4;

                index += shift >> 2;
            }
        }
        private void WriteBitDepth4LSB(IEnumerable<long> values, byte[] input)
        {
            var index = 0;
            var shift = 0;

            foreach (var value in values.Take(input.Length * 2))
            {
                input[index] |= (byte)(value << shift);
                index += shift >> 2;

                shift ^= 4;
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
