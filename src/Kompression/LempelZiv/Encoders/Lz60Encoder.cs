using System;
using System.Collections.Generic;
using System.IO;

namespace Kompression.LempelZiv.Encoders
{
    class Lz60Encoder : ILzEncoder
    {
        public void Encode(Stream input, Stream output, IMatch[] matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            new Lz40Encoder().WriteCompressedData(input, output, matches);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
