using System;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders.Level5
{
    public class RleEncoder : IEncoder
    {
        private readonly RleHeaderlessEncoder _encoder;

        public RleEncoder(IMatchParser matchParser)
        {
            _encoder = new RleHeaderlessEncoder(matchParser);
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0x1FFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new[] {
                (byte)((byte)(input.Length << 3) | 4),
                (byte)(input.Length >> 5),
                (byte)(input.Length >> 13),
                (byte)(input.Length >> 21) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output);
        }

        public void Dispose()
        {
        }
    }
}
