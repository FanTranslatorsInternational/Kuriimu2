using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    public class Mio0Decoder : IDecoder
    {
        private readonly ByteOrder _byteOrder;
        private CircularBuffer _circularBuffer;

        public Mio0Decoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Decode(Stream input, Stream output)
        {
            var inputStartPosition = input.Position;

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (!buffer.SequenceEqual(new byte[] { 0x4d, 0x49, 0x4f, 0x30 }))
                throw new InvalidCompressionException("MIO0" + (_byteOrder == ByteOrder.LittleEndian ? "LE" : "BE"));

            input.Read(buffer, 0, 4);
            var uncompressedLength = _byteOrder == ByteOrder.LittleEndian ? buffer.GetInt32LittleEndian(0) : buffer.GetInt32BigEndian(0);
            input.Read(buffer, 0, 4);
            var compressedTableOffset = _byteOrder == ByteOrder.LittleEndian ? buffer.GetInt32LittleEndian(0) : buffer.GetInt32BigEndian(0);
            input.Read(buffer, 0, 4);
            var uncompressedTableOffset = _byteOrder == ByteOrder.LittleEndian ? buffer.GetInt32LittleEndian(0) : buffer.GetInt32BigEndian(0);

            _circularBuffer =new CircularBuffer(0x1000);
            var compressedTablePosition = 0;
            var uncompressedTablePosition = 0;

            var bitLayout = new byte[compressedTableOffset - 0x10];
            input.Read(bitLayout, 0, bitLayout.Length);
            using (var bitReader = new BitReader(new MemoryStream(bitLayout), BitOrder.MsbFirst, 1, ByteOrder.BigEndian))
            {
                while (output.Length < uncompressedLength)
                {
                    if (bitReader.ReadBit() == 1)
                    {
                        // Flag for uncompressed byte
                        input.Position = inputStartPosition + uncompressedTableOffset + uncompressedTablePosition++;
                        var value = (byte)input.ReadByte();

                        output.WriteByte(value);
                        _circularBuffer.WriteByte(value);
                    }
                    else
                    {
                        // Flag for compressed data
                        input.Position = inputStartPosition + compressedTableOffset + compressedTablePosition;
                        var firstByte = input.ReadByte();
                        var secondByte = input.ReadByte();
                        compressedTablePosition += 2;

                        var length = (firstByte >> 4) + 3;
                        var displacement = (((firstByte & 0xF) << 8) | secondByte) + 1;

                        _circularBuffer.Copy(output,displacement,length);
                    }
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
