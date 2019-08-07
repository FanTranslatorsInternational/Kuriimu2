using System;
using System.IO;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public abstract class BaseLz : ICompression
    {
        protected abstract bool IsBackwards { get; }
        protected abstract ILzEncoder CreateEncoder();
        protected abstract ILzParser CreateParser(int inputLength);
        protected abstract ILzDecoder CreateDecoder();

        public void Decompress(Stream input, Stream output)
        {
            var decoder = CreateDecoder();

            decoder.Decode(input, output);

            decoder.Dispose();
        }

        public void Compress(Stream input, Stream output)
        {
            var encoder = CreateEncoder();
            var parser = CreateParser((int)input.Length);

            var inputArray = ToArray(input);
            if (IsBackwards)
                Array.Reverse(inputArray);
            var matches = parser.Parse(inputArray);
            if (IsBackwards)
            {
                Array.Reverse(matches);
                foreach (var match in matches)
                    match.SetPosition(input.Length - match.Position - 1);
            }

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
