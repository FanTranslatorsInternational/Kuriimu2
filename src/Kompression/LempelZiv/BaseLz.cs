using System.IO;
using System.Runtime.CompilerServices;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Parser;
[assembly: InternalsVisibleTo("KompressionUnitTests")]

namespace Kompression.LempelZiv
{
    public abstract class BaseLz : ICompression
    {
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

            encoder.Encode(input, output, parser.Parse(ToArray(input)));

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
