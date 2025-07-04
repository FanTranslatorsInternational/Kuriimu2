using System.Buffers.Binary;
using Kompression.Contract;

namespace plugin_most_wanted_ent.Compression
{
    class NintendoCompressor
    {
        public static void Decompress(Stream input, Stream output)
        {
            var method = (NintendoCompressionMethod)input.ReadByte();
            input.Position--;

            var configuration = GetConfiguration(method);

            configuration.Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, NintendoCompressionMethod method)
        {
            var configuration = GetConfiguration(method);

            configuration.Compress(input, output);
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

        public static ICompression GetConfiguration(NintendoCompressionMethod method)
        {
            switch (method)
            {
                case NintendoCompressionMethod.Lz10:
                    return Kompression.Compressions.Nintendo.Lz10.Build();

                case NintendoCompressionMethod.Lz11:
                    return Kompression.Compressions.Nintendo.Lz11.Build();

                case NintendoCompressionMethod.Lz40:
                    return Kompression.Compressions.Nintendo.Lz40.Build();

                case NintendoCompressionMethod.Lz60:
                    return Kompression.Compressions.Nintendo.Lz60.Build();

                case NintendoCompressionMethod.Huffman4:
                    return Kompression.Compressions.Nintendo.Huffman4Bit.Build();

                case NintendoCompressionMethod.Huffman8:
                    return Kompression.Compressions.Nintendo.Huffman8Bit.Build();

                case NintendoCompressionMethod.Rle:
                    return Kompression.Compressions.Nintendo.Rle.Build();

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }
        }
    }
}
