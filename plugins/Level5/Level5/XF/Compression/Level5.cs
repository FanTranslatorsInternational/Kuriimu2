using System;
using System.IO;
using System.Text;
using Komponent.IO;
using System.Linq;

namespace Kore.XFont.Compression
{
    public class Level5
    {
        public enum Method
        {
            NoCompression = 0,
            LZ10 = 1,
            Huffman4Bit = 2,
            Huffman8Bit = 3,
            RLE = 4
        }

        public static byte[] Decompress(Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.Default, true))
            {
                int sizeAndMethod = br.ReadInt32();
                int size = sizeAndMethod / 8;
                var method = (Method)(sizeAndMethod % 8);

                switch (method)
                {
                    case Method.NoCompression:
                        return br.ReadBytes(size);
                    case Method.LZ10:
                        return LZ10.Decompress(br.BaseStream, size);
                    case Method.Huffman4Bit:
                    case Method.Huffman8Bit:
                        int num_bits = method == Method.Huffman4Bit ? 4 : 8;
                        return Huffman.Decompress(br.BaseStream, num_bits, size, ByteOrder.LittleEndian);
                    case Method.RLE:
                        return RLE.Decompress(br.BaseStream, size);
                    default:
                        throw new NotSupportedException($"Unknown compression method {method}");
                }
            }
        }

        public static byte[] Compress(Stream stream, Method method)
        {
            if (stream.Length > 0x1fffffff)
                throw new Exception("File is too big to be compressed with Level5 compressions!");

            uint methodSize = (uint)stream.Length << 3;
            switch (method)
            {
                case Method.NoCompression:
                    using (var bw = new BinaryWriterX(new MemoryStream()))
                    {
                        bw.Write(methodSize);
                        stream.Position = 0;
                        stream.CopyTo(bw.BaseStream);
                        bw.BaseStream.Position = 0;
                        return new BinaryReaderX(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                    }
                case Method.LZ10:
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
                case Method.Huffman4Bit:
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
                case Method.Huffman8Bit:
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
                case Method.RLE:
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
    }
}
