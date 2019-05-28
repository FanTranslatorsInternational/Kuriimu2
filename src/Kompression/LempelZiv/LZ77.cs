using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Is more LZSS through the flag denoting if following data is compressed or raw.
   Though the format is denoted as LZ77 with the magic num? (Issue 517)*/

namespace Kompression.LempelZiv
{
    public static class LZ77
    {
        public static void Decompress(Stream input, Stream output)
        {
            var windowBuffer = new byte[0xFF];
            int windowBufferOffset = 0;

            var bitReader = new BitReader(input, BitOrder.LSBFirst);
            while (bitReader.Length - bitReader.Position >= 9)
            {
                if (bitReader.ReadBit() == 0)
                {
                    var nextByte = (byte)bitReader.ReadByte();

                    output.WriteByte(nextByte);
                    windowBuffer[windowBufferOffset] = nextByte;
                    windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                }
                else
                {
                    var displacement = bitReader.ReadByte();
                    var length = bitReader.ReadByte();
                    var nextByte = (byte)bitReader.ReadByte();

                    var bufferIndex = windowBufferOffset + windowBuffer.Length - displacement;
                    for (var i = 0; i < length; i++)
                    {
                        var next = windowBuffer[bufferIndex++ % windowBuffer.Length];
                        output.WriteByte(next);
                        windowBuffer[windowBufferOffset] = next;
                        windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                    }

                    // If an occurrence goes until the end of the file, 'next' still exists
                    // In this case, 'next' shouldn't be written to the file
                        // but there is no indicator if this symbol has to be written or not
                    // According to Kuriimu, I once implemented a 0x24 static symbol for some reason
                    // Maybe 'next' is 0x24 if an occurrence goes directly until the end of a file?
                    // TODO: Fix overflowing symbol
                    // HINT: Look at Kuriimu issue 517 and see if the used compression is this one
                    output.WriteByte(nextByte);
                    windowBuffer[windowBufferOffset] = nextByte;
                    windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                }
            }
        }

        public static void Compress(Stream input, Stream output)
        {
            var inputBuffer = new byte[input.Length - input.Position];
            var inputPosBk = input.Position;
            input.Read(inputBuffer, 0, inputBuffer.Length);
            input.Position = inputPosBk;
            var lzResults = Common.FindOccurrences(inputBuffer, 0xFF, 1, 0xFF, 1).
                OrderBy(x => x.Position).
                ToList();

            WriteCompressedData(input, output, lzResults);
        }

        private static void WriteCompressedData(Stream input, Stream output, IList<LzResult> lzResults)
        {
            var bw = new BitWriter(output, BitOrder.LSBFirst);

            var lzIndex = 0;
            while (input.Position < input.Length)
            {
                if (lzIndex < lzResults.Count && input.Position == lzResults[lzIndex].Position)
                {
                    bw.WriteBit(1);
                    bw.WriteByte((byte)lzResults[lzIndex].Displacement);
                    bw.WriteByte(lzResults[lzIndex].Length);
                    bw.WriteByte(lzResults[lzIndex].DiscrepancyBuffer[0]);
                    input.Position += lzResults[lzIndex].Length + lzResults[lzIndex].DiscrepancyBuffer.Length;
                    lzIndex++;
                }
                else
                {
                    bw.WriteBit(0);
                    bw.WriteByte(input.ReadByte());
                }
            }

            bw.Flush();
        }
    }
}
