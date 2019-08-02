using System.IO;
using Kompression.RunLengthEncoding.Decoders;
using Kompression.RunLengthEncoding.Encoders;
using Kompression.RunLengthEncoding.RleMatchFinders;

namespace Kompression.RunLengthEncoding
{
    public abstract class BaseRle : ICompression
    {
        protected abstract IRleEncoder CreateEncoder();
        protected abstract IRleMatchFinder CreateMatchFinder();
        protected abstract IRleDecoder CreateDecoder();

        public void Decompress(Stream input, Stream output)
        {
            var decoder = CreateDecoder();

            decoder.Decode(input, output);

            decoder.Dispose();
        }

        public void Compress(Stream input, Stream output)
        {
            var inputArray = ToArray(input);

            var encoder = CreateEncoder();
            var matchFinder = CreateMatchFinder();

            encoder.Encode(input, output, matchFinder.FindAllMatches(inputArray));

            encoder.Dispose();
            matchFinder.Dispose();
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
