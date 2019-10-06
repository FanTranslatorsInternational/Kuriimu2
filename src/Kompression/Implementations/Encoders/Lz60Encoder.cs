using System;
using System.IO;
using Kompression.Configuration;
using Kompression.Interfaces;

namespace Kompression.Implementations.Encoders
{
    public class Lz60Encoder : IEncoder
    {
        private Lz40Encoder _lz40Encoder;

        public Lz60Encoder(IMatchParser matchParser)
        {
            _lz40Encoder = new Lz40Encoder(matchParser);
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _lz40Encoder.WriteCompressedData(input, output);
        }

        public void Dispose()
        {
            _lz40Encoder?.Dispose();
            _lz40Encoder = null;
        }
    }
}
