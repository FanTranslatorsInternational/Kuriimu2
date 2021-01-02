using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Lz10Encoder : ILzEncoder
    {
        private readonly Lz10HeaderlessEncoder _encoder;

        public Lz10Encoder()
        {
            _encoder = new Lz10HeaderlessEncoder();
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz10PriceCalculator())
                .FindWith((options, limits) => new HistoryMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x10, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, matches);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
