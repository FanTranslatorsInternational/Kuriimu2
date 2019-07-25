using System.IO;

namespace Kompression.LempelZiv.Encoders
{
    class Lz77Encoder : ILzEncoder
    {
        public void Encode(Stream input, Stream output, LzMatch[] matches)
        {
            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, LzMatch[] lzResults)
        {
            using (var bw = new BitWriter(output, BitOrder.LSBFirst))
            {
                var lzIndex = 0;
                while (input.Position < input.Length)
                {
                    if (lzIndex < lzResults.Length && input.Position == lzResults[lzIndex].Position)
                    {
                        bw.WriteBit(1);
                        bw.WriteByte((byte)lzResults[lzIndex].Displacement);
                        bw.WriteByte(lzResults[lzIndex].Length);

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
