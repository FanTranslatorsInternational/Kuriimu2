using System;
using System.IO;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class RevLz77 : ICompression
    {
        public void Decompress(Stream input, Stream output)
        {
            var decoder = new RevLz77Decoder(ByteOrder.LittleEndian);

            decoder.Decode(input, output);

            decoder.Dispose();
        }

        public void Compress(Stream input, Stream output)
        {
            var encoder = new RevLz77Encoder();
            var parser = new PlusOneGreedyParser(new NeedleHaystackMatchFinder(3, 0x12, 0x1002));

            // Reverse input
            var inputArray = ToArray(input);
            Array.Reverse(inputArray);

            // Get matches; All information in them are backwards relative to the end
            // For example position of 0 relates to input.Length
            var matches = parser.Parse(new Span<byte>(inputArray));

            encoder.Encode(input, output, matches);

            encoder.Dispose();
            parser.Dispose();
        }

        private byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length];
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            return inputArray;
        }
    }
}
