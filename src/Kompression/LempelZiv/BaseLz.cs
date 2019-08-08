using System;
using System.IO;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public abstract class BaseLz : ICompression
    {
        protected virtual bool IsBackwards => false;
        protected virtual int PreBufferLength => 0;

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

            // Allocate array for input
            var inputArray = ToArray(input);

            // Parse matches
            var matches = parser.Parse(inputArray, PreBufferLength);
            if (IsBackwards)
            {
                Array.Reverse(matches);
                foreach (var match in matches)
                    match.SetPosition(input.Length - match.Position - 1);
            }

            // Encode matches and remaining raw data
            encoder.Encode(input, output, matches);

            // Dispose of objects
            encoder.Dispose();
            parser.Dispose();
        }

        private byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length + PreBufferLength];
            var offset = IsBackwards ? 0 : PreBufferLength;

            input.Read(inputArray, offset, inputArray.Length-offset);
            if (IsBackwards)
                Array.Reverse(inputArray);

            input.Position = bkPos;
            return inputArray;
        }
    }
}
