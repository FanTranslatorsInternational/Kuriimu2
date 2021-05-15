using System;
using System.Buffers.Binary;
using System.IO;
using Kontract.Kompression.Configuration;

namespace plugin_cattle_call.Compression
{
    class NintendoCompressor
    {
        public static void Decompress(Stream input, Stream output)
        {
            var method = (NintendoCompressionMethod)input.ReadByte();
            input.Position--;

            var configuration = GetConfiguration(method);

            configuration.Build().Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, NintendoCompressionMethod method)
        {
            var configuration = GetConfiguration(method);

            configuration.Build().Compress(input, output);
        }

        public static int PeekDecompressedSize(Stream input)
        {
            var sizeMethodBuffer = new byte[4];
            input.Read(sizeMethodBuffer, 0, 4);
            input.Position -= 4;

            return (int)(BinaryPrimitives.ReadUInt32LittleEndian(sizeMethodBuffer) >> 8);
        }

        public static NintendoCompressionMethod PeekCompressionMethod(Stream input)
        {
            var method = input.ReadByte();
            input.Position--;

            return (NintendoCompressionMethod)method;
        }

        public static IKompressionConfiguration GetConfiguration(NintendoCompressionMethod method)
        {
            switch (method)
            {
                case NintendoCompressionMethod.Lz10:
                    return Kompression.Implementations.Compressions.Nintendo.Lz10;

                case NintendoCompressionMethod.Lz11:
                    return Kompression.Implementations.Compressions.Nintendo.Lz11;

                case NintendoCompressionMethod.Lz40:
                    return Kompression.Implementations.Compressions.Nintendo.Lz40;

                case NintendoCompressionMethod.Lz60:
                    return Kompression.Implementations.Compressions.Nintendo.Lz60;

                case NintendoCompressionMethod.Huffman4:
                    return Kompression.Implementations.Compressions.Nintendo.Huffman4Bit;

                case NintendoCompressionMethod.Huffman8:
                    return Kompression.Implementations.Compressions.Nintendo.Huffman8Bit;

                case NintendoCompressionMethod.Rle:
                    return Kompression.Implementations.Compressions.Nintendo.Rle;

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }
        }
    }
}
