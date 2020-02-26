using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract;
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

        public string FormatName { get; }

        protected PixelEncoding(IPixelDescriptor pixelDescriptor, ByteOrder byteOrder)
        {
            ContractAssertions.IsNotNull(pixelDescriptor, nameof(pixelDescriptor));

            _descriptor = pixelDescriptor;
            _byteOrder = byteOrder;

            BitDepth = pixelDescriptor.GetBitDepth();
            FormatName = pixelDescriptor.GetPixelName();

            SetValueDelegates(BitDepth);
        }

        public IEnumerable<Color> Load(byte[] input, int taskCount)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            return ReadValues(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(_descriptor.GetColor);
        }

        public byte[] Save(IEnumerable<Color> colors, int taskCount)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            var values = colors.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(_descriptor.GetValue);

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
            var bytesToRead = (bitDepth + 7) / 8;

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
