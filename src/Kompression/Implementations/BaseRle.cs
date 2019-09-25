using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.PatternMatch;

namespace Kompression.Implementations
{
    public abstract class BaseRle : ICompression
    {
        protected abstract IPatternMatchEncoder CreateEncoder();
        protected abstract IMatchFinder CreateMatchFinder();
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
            var inputArray = ToArray(input);

            var encoder = CreateEncoder();
            var matchFinder = CreateMatchFinder();

            var matches = new List<Match>();
            for (var i = 0; i < inputArray.Length;)
            {
                var match = matchFinder.FindMatches(inputArray, i).FirstOrDefault();
                if (match != null)
                {
                    matches.Add(match);
                    i += (int)match.Length;
                }
                else
                {
                    i++;
                }
            }

            encoder.Encode(input, output, matches.ToArray());

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
