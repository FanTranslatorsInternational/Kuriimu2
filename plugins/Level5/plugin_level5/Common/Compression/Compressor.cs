using System.Buffers.Binary;
using Kompression.Contract;

namespace plugin_level5.Common.Compression
{
    public class Compressor
    {
        public MemoryStream Compress(Stream input, Level5CompressionMethod compressionType)
        {
            ICompression compression;

            MemoryStream ms = new();
            switch (compressionType)
            {
                case Level5CompressionMethod.NoCompression:
                    WriteCompressionMethod(ms, input, compressionType);

                    input.CopyTo(ms);
                    break;

                case Level5CompressionMethod.Lz10:
                    compression = Kompression.Compressions.Level5.Lz10.Build();
                    compression.Compress(input, ms);
                    break;

                case Level5CompressionMethod.Huffman4Bit:
                    compression = Kompression.Compressions.Level5.Huffman4Bit.Build();
                    compression.Compress(input, ms);
                    break;

                case Level5CompressionMethod.Huffman8Bit:
                    compression = Kompression.Compressions.Level5.Huffman8Bit.Build();
                    compression.Compress(input, ms);
                    break;

                case Level5CompressionMethod.Rle:
                    compression = Kompression.Compressions.Level5.Rle.Build();
                    compression.Compress(input, ms);
                    break;

                case Level5CompressionMethod.ZLib:
                    WriteCompressionMethod(ms, input, compressionType);

                    compression = Kompression.Compressions.ZLib.Build();
                    compression.Compress(input, ms);
                    break;
            }

            ms.Position = 0;
            return ms;
        }

        private void WriteCompressionMethod(Stream output, Stream input, Level5CompressionMethod compressionType)
        {
            uint compressionMethod = (uint)(input.Length << 3) | (uint)compressionType;

            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, compressionMethod);

            output.Write(buffer);
        }
    }
}
