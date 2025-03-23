using System.Buffers.Binary;
using Komponent.Streams;
using Kompression.Contract;

namespace plugin_level5.Common.Compression
{
    public class Decompressor
    {
        public void Decompress(Stream input, Stream output, long offset)
        {
            ICompression compression;

            input.Position = offset;

            Level5CompressionMethod method = PeekCompressionMethod(input, out int decompressedSize);

            switch (method)
            {
                case Level5CompressionMethod.NoCompression:
                    Stream subStream = new SubStream(input, offset + 4, decompressedSize);
                    subStream.CopyTo(output);
                    break;

                case Level5CompressionMethod.Lz10:
                    compression = Kompression.Compressions.Level5.Lz10.Build();
                    compression.Decompress(input, output);
                    break;

                case Level5CompressionMethod.Huffman4Bit:
                    compression = Kompression.Compressions.Level5.Huffman4Bit.Build();
                    compression.Decompress(input, output);
                    break;

                case Level5CompressionMethod.Huffman8Bit:
                    compression = Kompression.Compressions.Level5.Huffman8Bit.Build();
                    compression.Decompress(input, output);
                    break;

                case Level5CompressionMethod.Rle:
                    compression = Kompression.Compressions.Level5.Rle.Build();
                    compression.Decompress(input, output);
                    break;

                case Level5CompressionMethod.ZLib:
                    Stream zlibStream = new SubStream(input, offset + 4);

                    compression = Kompression.Compressions.ZLib.Build();
                    compression.Decompress(zlibStream, output);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown compression method {method}.");
            }

            output.Position = 0;
        }

        public Stream Decompress(Stream input, long offset)
        {
            MemoryStream ms = new();

            Decompress(input, ms, offset);

            return ms;
        }

        public Level5CompressionMethod PeekCompressionType(Stream input, long offset)
        {
            long bkPos = input.Position;
            input.Position = offset;

            Level5CompressionMethod type = PeekCompressionMethod(input, out _);

            input.Position = bkPos;
            return type;
        }

        private Level5CompressionMethod PeekCompressionMethod(Stream input, out int decompressedSize)
        {
            var buffer = new byte[4];

            int _ = input.Read(buffer);
            input.Position -= buffer.Length;

            uint methodSize = BinaryPrimitives.ReadUInt32LittleEndian(buffer);

            decompressedSize = (int)(methodSize >> 3);
            return (Level5CompressionMethod)(methodSize & 0x7);
        }
    }
}
