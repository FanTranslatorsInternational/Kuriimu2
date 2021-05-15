using System;
using System.IO;
using System.Linq;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class IecpDecoder : IDecoder
    {
        private Lzss01HeaderlessDecoder _decoder;

        public IecpDecoder()
        {
            _decoder = new Lzss01HeaderlessDecoder();
        }

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (!buffer.SequenceEqual(new byte[] { 0x49, 0x45, 0x43, 0x50 }))
                throw new InvalidCompressionException("IECP");

            input.Read(buffer, 0, 4);
            var decompressedSize = buffer.GetInt32LittleEndian(0);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
