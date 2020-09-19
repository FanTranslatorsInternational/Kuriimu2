using System;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Lz10Encoder : IEncoder
    {
        private readonly Lz10HeaderlessEncoder _encoder;

        public Lz10Encoder(IMatchParser matchParser)
        {
            _encoder = new Lz10HeaderlessEncoder(matchParser);
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x10, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input,output);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
