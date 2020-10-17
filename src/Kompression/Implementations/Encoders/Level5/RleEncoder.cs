using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Level5
{
    public class RleEncoder : ILzEncoder
    {
        private readonly RleHeaderlessEncoder _encoder;

        public RleEncoder()
        {
            _encoder = new RleHeaderlessEncoder();
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new NintendoRlePriceCalculator())
                .FindWith((options, limits) => new RleMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(0x3, 0x82));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0x1FFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new[] {
                (byte)((byte)(input.Length << 3) | 4),
                (byte)(input.Length >> 5),
                (byte)(input.Length >> 13),
                (byte)(input.Length >> 21) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, matches);
        }

        public void Dispose()
        {
        }
    }
}
