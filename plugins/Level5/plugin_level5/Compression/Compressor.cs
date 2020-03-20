using System;
using System.IO;
using Kontract.Kompression.Configuration;

namespace plugin_level5.Compression
{
    static class Compressor
    {
        public static void Decompress(Stream input, Stream output)
        {
            var method = (CompressionMethod)(input.ReadByte() & 0x7);
            input.Position--;

            IKompressionConfiguration configuration;
            switch (method)
            {
                case CompressionMethod.NoCompression:
                    input.Position += 4;
                    input.CopyTo(output);
                    return;

                case CompressionMethod.Lz10:
                    configuration = Kompression.Implementations.Compressions.Level5.Lz10;
                    break;

                case CompressionMethod.Huffman4Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman4Bit;
                    break;

                case CompressionMethod.Huffman8Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman8Bit;
                    break;

                case CompressionMethod.Rle:
                    configuration = Kompression.Implementations.Compressions.Level5.Rle;
                    break;

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }

            configuration.Build().Decompress(input, output);
        }

        public static void Compress(Stream input, Stream output, CompressionMethod method)
        {
            IKompressionConfiguration configuration;
            switch (method)
            {
                case CompressionMethod.NoCompression:
                    var compressionHeader = new[] {
                        (byte)(input.Length << 3),
                        (byte)(input.Length >> 5),
                        (byte)(input.Length >> 13),
                        (byte)(input.Length >> 21) };
                    output.Write(compressionHeader, 0, 4);

                    input.CopyTo(output);
                    return;

                case CompressionMethod.Lz10:
                    configuration = Kompression.Implementations.Compressions.Level5.Lz10;
                    break;

                case CompressionMethod.Huffman4Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman4Bit;
                    break;

                case CompressionMethod.Huffman8Bit:
                    configuration = Kompression.Implementations.Compressions.Level5.Huffman8Bit;
                    break;

                case CompressionMethod.Rle:
                    configuration = Kompression.Implementations.Compressions.Level5.Rle;
                    break;

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }

            configuration.Build().Compress(input, output);
        }
    }
}
