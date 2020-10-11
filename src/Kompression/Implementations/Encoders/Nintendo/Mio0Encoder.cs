using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kompression.Extensions;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Mio0Encoder : ILzEncoder
    {
        private readonly ByteOrder _byteOrder;

        public Mio0Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var bitLayoutStream = new MemoryStream();
            var compressedTableStream = new MemoryStream();
            var uncompressedTableStream = new MemoryStream();

            using var bitLayoutWriter = new BitWriter(bitLayoutStream, BitOrder.MostSignificantBitFirst, 1, ByteOrder.BigEndian);
            using var bwCompressed = new BinaryWriter(compressedTableStream, Encoding.ASCII, true);
            using var bwUncompressed = new BinaryWriter(uncompressedTableStream, Encoding.ASCII, true);

            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position)
                {
                    bitLayoutWriter.WriteBit(1);
                    bwUncompressed.Write((byte)input.ReadByte());
                }

                // Write match data to the compressed table
                var firstByte = (byte)((byte)((match.Length - 3) << 4) | (byte)((match.Displacement - 1) >> 8));
                var secondByte = (byte)(match.Displacement - 1);
                bitLayoutWriter.WriteBit(0);
                bwCompressed.Write(firstByte);
                bwCompressed.Write(secondByte);

                input.Position += match.Length;
            }

            // Write any data after last match, to the uncompressed table
            while (input.Position < input.Length)
            {
                bitLayoutWriter.WriteBit(1);
                bwUncompressed.Write((byte)input.ReadByte());
            }

            bitLayoutWriter.Flush();

            WriteCompressedData(input, output, bitLayoutStream, compressedTableStream, uncompressedTableStream);
        }

        private void WriteCompressedData(Stream input, Stream output, Stream bitLayoutStream, Stream compressedTableStream, Stream uncompressedTableStream)
        {
            // Create header values
            var uncompressedLength = _byteOrder == ByteOrder.LittleEndian
                ? ((int)input.Length).GetArrayLittleEndian()
                : ((int)input.Length).GetArrayBigEndian();
            var compressedTableOffsetInt = (int)(0x10 + ((bitLayoutStream.Length + 3) & ~3));
            var compressedTableOffset = _byteOrder == ByteOrder.LittleEndian
                ? compressedTableOffsetInt.GetArrayLittleEndian()
                : compressedTableOffsetInt.GetArrayBigEndian();
            var uncompressedTableOffset = _byteOrder == ByteOrder.LittleEndian
                ? ((int)(compressedTableOffsetInt + compressedTableStream.Length)).GetArrayLittleEndian()
                : ((int)(compressedTableOffsetInt + compressedTableStream.Length)).GetArrayBigEndian();

            // Write header
            output.Write(Encoding.ASCII.GetBytes("MIO0"), 0, 4);
            output.Write(uncompressedLength, 0, 4);
            output.Write(compressedTableOffset, 0, 4);
            output.Write(uncompressedTableOffset, 0, 4);

            // Write data streams
            bitLayoutStream.Position = 0;
            bitLayoutStream.CopyTo(output);
            output.Position = (output.Position + 3) & ~3;

            compressedTableStream.Position = 0;
            compressedTableStream.CopyTo(output);

            uncompressedTableStream.Position = 0;
            uncompressedTableStream.CopyTo(output);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
