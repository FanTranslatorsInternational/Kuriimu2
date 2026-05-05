using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder
{
    public class CrilaylaDecoder : IDecoder
    {
        private const int RawSize_ = 0x100;

        public void Decode(Stream input, Stream output)
        {
            using var br = new BinaryReaderX(input, true);

            var header = ReadHeader(br);
            if (header.Magic != "CRILAYLA" || header.Magic == "\0\0\0\0\0\0\0\0")
                throw new InvalidCompressionException("Crilayla");

            // Copy raw part
            input.Position = input.Length - RawSize_;
            output.Write(br.ReadBytes(RawSize_), 0, RawSize_);

            // Decompress
            var compStream = new SubStream(input, 0x10, input.Length - RawSize_ - 0x10);
            var reverseCompStream = new ReverseStream(compStream, compStream.Length);
            var reverseOutputStream = new ReverseStream(output, header.DecompSize + RawSize_);
            var circularBuffer = new CircularBuffer(0x2002);

            using var reverseBr = new BinaryReaderX(reverseCompStream, ByteOrder.LittleEndian, BitOrder.MostSignificantBitFirst, 1);

            while (reverseOutputStream.Position < reverseOutputStream.Length - RawSize_)
            {
                if (reverseBr.ReadBit() != 1)
                {
                    var value = reverseBr.ReadBits<byte>(8);

                    reverseOutputStream.WriteByte(value);
                    circularBuffer.WriteByte(value);
                    continue;
                }

                var displacement = reverseBr.ReadBits<short>(13) + 3;
                var length = ReadLength(reverseBr) + 3;

                circularBuffer.Copy(reverseOutputStream, displacement, length);
            }

        }

        private static int ReadLength(BinaryReaderX br)
        {
            var length = br.ReadBits<int>(2);
            if (length != 3)
                return length;

            var more = br.ReadBits<int>(3);
            length += more;
            if (more != 7)
                return length;

            more = br.ReadBits<int>(5);
            length += more;
            if (more != 31)
                return length;

            while ((more = br.ReadBits<int>(8)) == 0xFF)
                length += more;

            return length + more;
        }

        private static CrilaylaHeader ReadHeader(BinaryReaderX br)
        {
            return new CrilaylaHeader
            {
                Magic = br.ReadString(8),
                DecompSize = br.ReadInt32(),
                CompSize = br.ReadInt32()
            };
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    internal struct CrilaylaHeader
    {
        public string Magic;
        public int DecompSize;
        public int CompSize;
    }
}
