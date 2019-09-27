using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Configuration;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class Lz60Encoder : IEncoder
    {
        private IMatchParser _matchParser;

        public Lz60Encoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            var lz40Encoder = new Lz40Encoder(_matchParser);
            lz40Encoder.WriteCompressedData(input, output);
            lz40Encoder.Dispose();
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
