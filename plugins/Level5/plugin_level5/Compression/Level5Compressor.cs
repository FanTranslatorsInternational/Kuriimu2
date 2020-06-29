using System;
using System.Buffers.Binary;
using System.IO;
using Kontract.Kompression.Configuration;

namespace plugin_level5.Compression
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

        public static void Decompress(Stream input, Stream output)
        {
            var method = (Level5CompressionMethod)(input.ReadByte() & 0x7);
            input.Position--;

            IKompressionConfiguration configuration;
            switch (method)
            {
                case Level5CompressionMethod.NoCompression:
                    input.Position += 4;
                    input.CopyTo(output);
                    return;

                case Level5CompressionMethod.Lz10:
                    configuration = Kompression.Implementations.Compressions.Level5.Lz10;
                    break;

                case Level5CompressionMethod.Huffman4Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman4Bit;
                    break;

                case Level5CompressionMethod.Huffman8Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman8Bit;
                    break;

                case Level5CompressionMethod.Rle:
                    configuration = Kompression.Implementations.Compressions.Level5.Rle;
                    break;

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }

            configuration.Build().Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, Level5CompressionMethod method)
        {
            IKompressionConfiguration configuration;
            switch (method)
            {
                case Level5CompressionMethod.NoCompression:
                    var compressionHeader = new[] {
                        (byte)(input.Length << 3),
                        (byte)(input.Length >> 5),
                        (byte)(input.Length >> 13),
                        (byte)(input.Length >> 21) };
                    output.Write(compressionHeader, 0, 4);

                    input.CopyTo(output);
                    return;

                case Level5CompressionMethod.Lz10:
                    configuration = Kompression.Implementations.Compressions.Level5.Lz10;
                    break;

                case Level5CompressionMethod.Huffman4Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman4Bit;
                    break;

                case Level5CompressionMethod.Huffman8Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman8Bit;
                    break;

                case Level5CompressionMethod.Rle:
                    configuration = Kompression.Implementations.Compressions.Level5.Rle;
                    break;

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }

            configuration.Build().Compress(input, output);
        }
    }
}
