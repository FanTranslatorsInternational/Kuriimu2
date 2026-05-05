using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Contract.Encoding.Descriptor;
using Komponent.Contract.Enums;
using Komponent.IO;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.Base
{
    public abstract class PixelIndexEncoding : IIndexEncoding
    {
        private readonly IPixelIndexDescriptor _descriptor;
        private readonly ByteOrder _byteOrder;
        private readonly BitOrder _bitOrder;

        private readonly Func<BinaryReaderX, IList<long>> _readValuesDelegate;
        private readonly Action<BinaryWriterX, long> _writeValueDelegate;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

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
            BitsPerValue = BitDepth;
            ColorsPerValue = 1;

            GetValueDelegates(BitDepth, out _readValuesDelegate, out _writeValueDelegate);
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Rgba32> Load(byte[] input, IList<Rgba32> palette, EncodingOptions loadContext)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder, _bitOrder, 1);

            return ReadValues(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .Select(i => _descriptor.GetColor(i, palette));
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<int> indices, IList<Rgba32> palette, EncodingOptions saveContext)
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

        private static void GetValueDelegates(int bitDepth, out Func<BinaryReaderX, IList<long>> readDelegate, out Action<BinaryWriterX, long> writeDelegate)
        {
            switch (bitDepth)
            {
                case 1:
                    readDelegate = br => ReadBitValues(br, bitDepth);
                    writeDelegate = (bw, value) => bw.WriteBits(value, 1);
                    break;

                case 2:
                    readDelegate = br => ReadBitValues(br, bitDepth);
                    writeDelegate = (bw, value) => bw.WriteBits(value, 2);
                    break;

                case 4:
                    readDelegate = br => ReadBitValues(br, bitDepth);
                    writeDelegate = (bw, value) => bw.WriteBits(value, 4);
                    break;

                case 8:
                    readDelegate = br => [br.ReadByte()];
                    writeDelegate = (bw, value) => bw.Write((byte)value);
                    break;

                case 16:
                    readDelegate = br => [br.ReadUInt16()];
                    writeDelegate = (bw, value) => bw.Write((ushort)value);
                    break;

                default:
                    throw new InvalidOperationException($"BitDepth {bitDepth} not supported.");
            }
        }

        private static long[] ReadBitValues(BinaryReaderX br, int bitLength)
        {
            var valueCount = (br.BlockSize * 8 + (bitLength - 1)) / bitLength;
            var result = new long[valueCount];

            for (var i = 0; i < valueCount; i++)
                result[i] = br.ReadBits<long>(bitLength);

            return result;
        }
    }
}
