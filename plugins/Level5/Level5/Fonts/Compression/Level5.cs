using System;
using System.IO;
using System.Text;
using Komponent.IO;

namespace Level5.Fonts.Compression
{
    public class Compressor
    {
        public static byte[] Decompress(Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.Default, true))
            {
                int sizeAndMethod = br.ReadInt32();
                int size = sizeAndMethod / 8;
                var method = (CompressionMethod)(sizeAndMethod % 8);

                switch (method)
                {
                    case CompressionMethod.NoCompression:
                        return br.ReadBytes(size);
                    case CompressionMethod.LZ10:
                        return LZ10.Decompress(br.BaseStream, size);
                    case CompressionMethod.Huffman4Bit:
                    case CompressionMethod.Huffman8Bit:
                        int num_bits = method == CompressionMethod.Huffman4Bit ? 4 : 8;
                        return Huffman.Decompress(br.BaseStream, num_bits, size, ByteOrder.LittleEndian);
                    case CompressionMethod.RLE:
                        return RLE.Decompress(br.BaseStream, size);
                    default:
                        throw new NotSupportedException($"Unknown compression method {method}");
                }
            }
        }

        public static byte[] Compress(Stream stream, CompressionMethod method)
        {
            if (stream.Length > 0x1fffffff)
                throw new Exception("File is too big to be compressed with Level5 compressions!");

            if (stream.Length <= 0)
                return WriteUncompressed(stream, 0);

            uint methodSize = (uint)stream.Length << 3;
            switch (method)
            {
                case CompressionMethod.NoCompression:
                    return WriteUncompressed(stream, methodSize);
                case CompressionMethod.LZ10:
                    methodSize |= 0x1;
                    using (var bw = new BinaryWriterX(new MemoryStream()))
                    {
                        bw.Write(methodSize);
                        stream.Position = 0;
                        var comp = LZ10.Compress(stream);
                        bw.Write(comp);
                        bw.BaseStream.Position = 0;
                        return new BinaryReaderX(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                    }
                case CompressionMethod.Huffman4Bit:
                    methodSize |= 0x2;
                    using (var bw = new BinaryWriterX(new MemoryStream()))
                    {
                        bw.Write(methodSize);
                        stream.Position = 0;
                        var comp = Huffman.Compress(stream, 4);
                        bw.Write(comp);
                        bw.BaseStream.Position = 0;
                        return new BinaryReaderX(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                    }
                case CompressionMethod.Huffman8Bit:
                    methodSize |= 0x3;
                    using (var bw = new BinaryWriterX(new MemoryStream()))
                    {
                        bw.Write(methodSize);
                        stream.Position = 0;
                        var comp = Huffman.Compress(stream, 8);
                        bw.Write(comp);
                        bw.BaseStream.Position = 0;
                        return new BinaryReaderX(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                    }
                case CompressionMethod.RLE:
                    methodSize |= 0x4;
                    using (var bw = new BinaryWriterX(new MemoryStream()))
                    {
                        bw.Write(methodSize);
                        stream.Position = 0;
                        var comp = RLE.Compress(stream);
                        bw.Write(comp);
                        bw.BaseStream.Position = 0;
                        return new BinaryReaderX(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                    }
                default:
                    throw new Exception($"Unsupported compression method {method}!");
            }
        }

        private static byte[] WriteUncompressed(Stream input, uint methodSize)
        {
            using (var bw = new BinaryWriterX(new MemoryStream()))
            {
                bw.Write(methodSize);
                input.Position = 0;
                input.CopyTo(bw.BaseStream);
                bw.BaseStream.Position = 0;
                return (bw.BaseStream as MemoryStream)?.ToArray();
            }
        }
    }
}
