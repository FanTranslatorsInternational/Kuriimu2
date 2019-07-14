using System.IO;
using System.Runtime.CompilerServices;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;
[assembly:InternalsVisibleTo("KompressionUnitTests")]

namespace Kompression.LempelZiv
{
    public abstract class BaseLz : ICompression
    {
        protected abstract ILzMatchFinder CreateMatchFinder(int inputLength);
        protected abstract ILzEncoder CreateEncoder();
        protected abstract ILzParser CreateParser(ILzMatchFinder finder, ILzEncoder encoder);
        protected abstract ILzDecoder CreateDecoder();

        public void Decompress(Stream input, Stream output)
        {
            var decoder = CreateDecoder();
            decoder.Decode(input, output);
        }

        public void Compress(Stream input, Stream output)
        {
            var encoder = CreateEncoder();
            var parser = CreateParser(CreateMatchFinder((int)input.Length), encoder);
            encoder.Encode(input, output, parser.Parse(ToArray(input)));
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
