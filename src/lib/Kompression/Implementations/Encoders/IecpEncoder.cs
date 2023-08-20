using System.Collections.Generic;
using System.IO;

using Kompression.Extensions;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class IecpEncoder : ILzEncoder
    {
        private Lzss01HeaderlessEncoder _encoder;

        public IecpEncoder()
        {
            _encoder = new Lzss01HeaderlessEncoder();
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            output.Position += 8;

            _encoder.Encode(input, output, matches);

            WriteHeaderData(output, (int)input.Length);
        }

        private void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            Write(output, new byte[] { 0x49, 0x45, 0x43, 0x50 });
            Write(output, decompressedLength.GetArrayLittleEndian());

            output.Position = endPosition;
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
