using System.IO;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class ShadeLzDecoder : IDecoder
    {
        private readonly ShadeLzHeaderlessDecoder _decoder;

        public ShadeLzDecoder()
        {
            _decoder = new ShadeLzHeaderlessDecoder();
        }

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var magic = (uint)buffer.GetInt32BigEndian(0);
            if (magic != 0xFCAA55A7)
                throw new InvalidCompressionException("Spike Chunsoft");

            input.Read(buffer, 0, 4);
            var decompressedSize = buffer.GetInt32LittleEndian(0);
            input.Read(buffer, 0, 4);
            var compressedSize = buffer.GetInt32LittleEndian(0);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
        }
    }
}
