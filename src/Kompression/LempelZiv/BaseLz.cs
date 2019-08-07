using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Parser;
[assembly: InternalsVisibleTo("KompressionUnitTests")]

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
            encoder.Encode(input, output, parser.Parse(inputArray));

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
