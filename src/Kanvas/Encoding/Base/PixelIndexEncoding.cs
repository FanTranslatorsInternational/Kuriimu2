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
    public abstract class PixelIndexEncoding : IColorIndexEncoding
    {
        private readonly IPixelIndexDescriptor _descriptor;
        private readonly ByteOrder _byteOrder;

        private Func<BinaryReaderX, long> _readValueDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        public int BitDepth { get; }

        public string FormatName { get; }

        protected PixelIndexEncoding(IPixelIndexDescriptor pixelDescriptor, ByteOrder byteOrder)
        {
            _descriptor = pixelDescriptor;

            BitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();

            SetValueDelegates(BitDepth);
        }

        public IEnumerable<Color> Load(byte[] input, IList<Color> palette, int taskCount)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            return ReadValues(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(c => _descriptor.GetColor(c, palette));
        }

        public byte[] Save(IEnumerable<int> indices, IList<Color> palette, int taskCount)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

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
                yield return _readValueDelegate(br);
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
