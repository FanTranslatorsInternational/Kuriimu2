using System.IO;
using Kompression.IO;

namespace Kompression.LempelZiv.Encoders
{
    class Lz77Encoder : ILzEncoder
    {
        public void Encode(Stream input, Stream output, Match[] matches)
        {
            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, Match[] lzResults)
        {
            using (var bw = new BitWriter(output, BitOrder.LSBFirst, 1, ByteOrder.BigEndian))
            {
                var lzIndex = 0;
                while (input.Position < input.Length)
                {
                    if (lzIndex < lzResults.Length && input.Position == lzResults[lzIndex].Position)
                    {
                        bw.WriteBit(1);
                        bw.WriteByte((byte)lzResults[lzIndex].Displacement);
                        bw.WriteByte((int)lzResults[lzIndex].Length);

                        input.Position += lzResults[lzIndex].Length;
                        bw.WriteByte(input.ReadByte());

                        lzIndex++;
                    }
                    else
                    {
                        bw.WriteBit(0);
                        bw.WriteByte(input.ReadByte());
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
