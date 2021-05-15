using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Nintendo
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
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[]
            {
                0x30, (byte) (input.Length & 0xFF), (byte) ((input.Length >> 8) & 0xFF),
                (byte) ((input.Length >> 16) & 0xFF)
            };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, matches);
        }

        public void Dispose()
        {
        }
    }
}
