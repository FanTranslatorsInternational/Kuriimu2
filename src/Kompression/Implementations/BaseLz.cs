using System;
using System.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations
{
    public abstract class BaseLz : ICompression
    {
        protected virtual bool IsBackwards => false;
        protected virtual int PreBufferLength => 0;

        protected abstract IPatternMatchEncoder CreateEncoder();
        protected abstract IMatchParser CreateParser(int inputLength);
        protected abstract IPatternMatchDecoder CreateDecoder();

        public abstract string[] Names { get; }

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
            var matches = parser.ParseMatches(inputArray, PreBufferLength);

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

            input.Read(inputArray, offset, inputArray.Length - offset);
            if (IsBackwards)
                Array.Reverse(inputArray);

            input.Position = bkPos;
            return inputArray;
        }
    }
}
