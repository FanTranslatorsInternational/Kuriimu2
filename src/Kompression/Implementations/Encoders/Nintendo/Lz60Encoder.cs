using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Lz60Encoder : ILzEncoder
    {
        private Lz40Encoder _lz40Encoder;

        public Lz60Encoder()
        {
            _lz40Encoder = new Lz40Encoder();
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _lz40Encoder.WriteCompressedData(input, output, matches.ToArray());
        }

        public void Dispose()
        {
            _lz40Encoder?.Dispose();
            _lz40Encoder = null;
        }
    }
}
