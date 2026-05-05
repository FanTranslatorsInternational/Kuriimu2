using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder.Nintendo
{
    public class Mio0Decoder(ByteOrder byteOrder) : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            long inputStartPosition = input.Position;

            var buffer = new byte[4];

            _ = input.Read(buffer);
            if (buffer[0] is not 0x4D || buffer[1] is not 0x49 || buffer[2] is not 0x4F || buffer[3] is not 0x30)
                throw new InvalidCompressionException("MIO0" + (byteOrder == ByteOrder.LittleEndian ? "LE" : "BE"));

            using var br = new BinaryReaderX(input, true, byteOrder);

            int uncompressedLength = br.ReadInt32();
            int compressedTableOffset = br.ReadInt32();
            int uncompressedTableOffset = br.ReadInt32();

            var circularBuffer = new CircularBuffer(0x1000);
            var compressedTablePosition = 0;
            var uncompressedTablePosition = 0;

            var bitStream = new SubStream(input, input.Position, compressedTableOffset - 0x10);
            using var bitReader = new BinaryBitReader(bitStream, BitOrder.MostSignificantBitFirst, 1, ByteOrder.BigEndian);

            while (output.Length < uncompressedLength)
            {
                if (bitReader.ReadBit() == 1)
                {
                    // Flag for uncompressed byte
                    input.Position = inputStartPosition + uncompressedTableOffset + uncompressedTablePosition++;
                    var value = (byte)input.ReadByte();

                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    // Flag for compressed data
                    input.Position = inputStartPosition + compressedTableOffset + compressedTablePosition;
                    int firstByte = input.ReadByte();
                    int secondByte = input.ReadByte();
                    compressedTablePosition += 2;

                    int length = (firstByte >> 4) + 3;
                    int displacement = ((firstByte & 0xF) << 8 | secondByte) + 1;

                    circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
