using System.Collections.Generic;
using System.IO;
using Kompression.Extensions;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class ShadeLzEncoder : ILzEncoder
    {
        private readonly ShadeLzHeaderlessEncoder _encoder;

        public ShadeLzEncoder()
        {
            _encoder = new ShadeLzHeaderlessEncoder();
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            output.Position += 0xC;
            _encoder.Encode(input, output, matches);

            WriteHeaderData(output, input.Length);
        }

        private void WriteHeaderData(Stream output, long uncompressedLength)
        {
            var bkPos = output.Position;
            output.Position = 0;

            var magic = new byte[] { 0xFC, 0xAA, 0x55, 0xA7 };
            Write(output, magic);
            Write(output, ((int)uncompressedLength).GetArrayLittleEndian());
            Write(output, ((int)output.Length).GetArrayLittleEndian());

            output.Position = bkPos;
        }

        private void Write(Stream output, byte[] data)
        {
#if NET_CORE_31
            output.Write(data);
#else
            output.Write(data, 0, data.Length);
#endif
        }

        public void Dispose()
        {
        }
    }
}
