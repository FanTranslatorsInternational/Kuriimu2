﻿using System;
using System.Buffers.Binary;
using System.IO;
using Kompression.Extensions;
using Kompression.Implementations.Decoders.Headerless;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class TalesOf01Decoder : IDecoder
    {
        private Lzss01HeaderlessDecoder _decoder;

        public TalesOf01Decoder()
        {
            _decoder = new Lzss01HeaderlessDecoder();
        }

        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x01)
                throw new InvalidOperationException("This is not a tales of compression with version 1.");

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var compressedDataSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));
            input.Read(buffer, 0, 4);
            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
