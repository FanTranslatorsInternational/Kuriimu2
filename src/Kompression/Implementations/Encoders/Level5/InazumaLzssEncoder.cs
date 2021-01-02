using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Level5
{
    public class InazumaLzssEncoder : ILzEncoder
    {
        private Lzss01HeaderlessEncoder _encoder;

        public InazumaLzssEncoder()
        {
            _encoder = new Lzss01HeaderlessEncoder();
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            output.Position = 0x10;
            _encoder.Encode(input, output, matches);

            output.Position = 0;
            output.Write(new byte[] { 0x53, 0x53, 0x5A, 0x4C }, 0, 4);  // SSZL

            output.Position += 4;
            var compressedBuffer = new[]
            {
                (byte)output.Length,
                (byte)((output.Length>>8)&0xFF),
                (byte)((output.Length>>16)&0xFF),
                (byte)((output.Length>>24)&0xFF),
            };
            output.Write(compressedBuffer, 0, 4);

            var decompressedSizeBuffer = new[]
            {
                (byte)input.Length,
                (byte)((input.Length>>8)&0xFF),
                (byte)((input.Length>>16)&0xFF),
                (byte)((input.Length>>24)&0xFF),
            };
            output.Write(decompressedSizeBuffer, 0, 4);

            output.Position = output.Length;
        }

        public void Dispose()
        {
        }
    }
}
