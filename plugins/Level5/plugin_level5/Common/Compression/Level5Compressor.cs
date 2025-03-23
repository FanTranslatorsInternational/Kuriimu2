using System.Buffers.Binary;
using Komponent.Streams;
using Kompression.Contract;

namespace plugin_level5.Common.Compression
{
    public static class Level5Compressor
    {
        public static int PeekDecompressedSize(Stream input)
        {
            var sizeMethodBuffer = new byte[4];
            input.Read(sizeMethodBuffer, 0, 4);
            input.Position -= 4;

            return (int)(BinaryPrimitives.ReadUInt32LittleEndian(sizeMethodBuffer) >> 3);
        }

        public static Level5CompressionMethod PeekCompressionMethod(Stream input)
        {
            var sizeMethodBuffer = new byte[4];
            input.Read(sizeMethodBuffer, 0, 4);
            input.Position -= 4;

            return (Level5CompressionMethod)(BinaryPrimitives.ReadUInt32LittleEndian(sizeMethodBuffer) & 0x7);
        }

        public static ICompression? GetCompression(Level5CompressionMethod method)
        {
            switch (method)
            {
                case Level5CompressionMethod.NoCompression:
                    return null;

                case Level5CompressionMethod.Lz10:
                    return Kompression.Compressions.Level5.Lz10.Build();

                case Level5CompressionMethod.Huffman4Bit:
                    return Kompression.Compressions.Level5.Huffman4Bit.Build();

                case Level5CompressionMethod.Huffman8Bit:
                    return Kompression.Compressions.Level5.Huffman8Bit.Build();

                case Level5CompressionMethod.Rle:
                    return Kompression.Compressions.Level5.Rle.Build();

                case Level5CompressionMethod.ZLib:
                    return Kompression.Compressions.ZLib.Build();

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }
        }

        public static void Decompress(Stream input, Stream output)
        {
            var method = PeekCompressionMethod(input);
            if (method == Level5CompressionMethod.NoCompression)
            {
                input.Position += 4;
                input.CopyTo(output);
                return;
            }

            if (method == Level5CompressionMethod.ZLib)
                input = new SubStream(input, 4, input.Length - 4);

            ICompression? configuration = GetCompression(method);
            configuration?.Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, Level5CompressionMethod method)
        {
            var configuration = GetCompression(method);
            if (configuration == null)
            {
                var compressionHeader = new[] {
                    (byte)(input.Length << 3),
                    (byte)(input.Length >> 5),
                    (byte)(input.Length >> 13),
                    (byte)(input.Length >> 21) };
                output.Write(compressionHeader, 0, 4);

                input.CopyTo(output);
                return;
            }

            if (method == Level5CompressionMethod.ZLib)
            {
                var compressionHeader = new[] {
                    (byte) (input.Length << 3 | 5),
                    (byte) (input.Length >> 5),
                    (byte) (input.Length >> 13),
                    (byte) (input.Length >> 21) };
                output.Write(compressionHeader, 0, 4);
            }

            configuration.Compress(input, output);
        }
    }
}
