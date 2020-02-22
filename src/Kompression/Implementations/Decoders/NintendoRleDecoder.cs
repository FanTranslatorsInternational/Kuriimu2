using System.IO;
using System.Linq;
using Kompression.Exceptions;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class NintendoRleDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x30)
                throw new InvalidCompressionException("NintendoRle");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            while (output.Length < decompressedSize)
            {
                var flag = input.ReadByte();
                if ((flag & 0x80) > 0)
                {
                    var repetitions = (flag & 0x7F) + 3;
                    output.Write(Enumerable.Repeat((byte)input.ReadByte(), repetitions).ToArray(), 0, repetitions);
                }
                else
                {
                    var length = flag + 1;
                    var uncompressedData = new byte[length];
                    input.Read(uncompressedData, 0, length);
                    output.Write(uncompressedData, 0, length);
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
