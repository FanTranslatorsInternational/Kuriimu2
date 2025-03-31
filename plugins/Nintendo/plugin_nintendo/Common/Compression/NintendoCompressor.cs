using Kompression.Contract;
using System.Buffers.Binary;

namespace plugin_nintendo.Common.Compression
{
    class NintendoCompressor
    {
        public static int PeekDecompressedSize(Stream input)
        {
            var sizeMethodBuffer = new byte[4];
            input.Read(sizeMethodBuffer, 0, 4);
            input.Position -= 4;

            return (int)(BinaryPrimitives.ReadUInt32LittleEndian(sizeMethodBuffer) >> 8);
        }

        public static NintendoCompressionMethod PeekCompressionMethod(Stream input)
        {
            var method = (byte)input.ReadByte();
            input.Position--;

            if (Enum.IsDefined(typeof(NintendoCompressionMethod), method))
                return (NintendoCompressionMethod)method;

            return NintendoCompressionMethod.Unsupported;
        }

        public static ICompression GetCompression(NintendoCompressionMethod method)
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

        public static void Decompress(Stream input, Stream output)
        {
            var method = (NintendoCompressionMethod)input.ReadByte();
            input.Position--;

            ICompression compression = GetCompression(method);

            compression.Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, NintendoCompressionMethod method)
        {
            ICompression compression = GetCompression(method);

            compression.Compress(input, output);
        }
    }
}
