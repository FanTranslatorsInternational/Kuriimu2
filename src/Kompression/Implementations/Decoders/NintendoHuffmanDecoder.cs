using System.IO;
using System.Linq;
using System.Text;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    class NintendoHuffmanDecoder : IDecoder
    {
        private readonly int _bitDepth;
        private readonly ByteOrder _byteOrder;

        public NintendoHuffmanDecoder(int bitDepth, ByteOrder byteOrder)
        {
            _bitDepth = bitDepth;
            _byteOrder = byteOrder;
        }

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x20 + _bitDepth)
                throw new InvalidCompressionException($"Huffman{_bitDepth}");

            var decompressedLength = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);
            var result = new byte[decompressedLength * 8 / _bitDepth];

            using (var br = new BinaryReader(input, Encoding.ASCII, true))
            {
                var treeSize = br.ReadByte();
                var treeRoot = br.ReadByte();
                var treeBuffer = br.ReadBytes(treeSize * 2);

                for (int i = 0, code = 0, next = 0, pos = treeRoot, resultPos = 0; resultPos < result.Length; i++)
                {
                    if (i % 32 == 0)
                        code = br.ReadInt32();

                    next += ((pos & 0x3F) << 1) + 2;
                    var direction = (code >> (31 - i)) % 2 == 0 ? 2 : 1;
                    var leaf = (pos >> 5 >> direction) % 2 != 0;

                    pos = treeBuffer[next - direction];
                    if (leaf)
                    {
                        result[resultPos++] = (byte)pos;
                        pos = treeRoot;
                        next = 0;
                    }
                }
            }

            if (_bitDepth == 8)
                output.Write(result, 0, result.Length);
            else
            {
                byte[] combinedData;
                if (_byteOrder == ByteOrder.LittleEndian)
                    combinedData = Enumerable.Range(0, decompressedLength).Select(j => (byte)(result[2 * j + 1] * 16 + result[2 * j])).ToArray();
                else
                    combinedData = Enumerable.Range(0, decompressedLength).Select(j => (byte)(result[2 * j] * 16 + result[2 * j + 1])).ToArray();
                output.Write(combinedData, 0, combinedData.Length);
            }
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
