using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Huffman.Support;
using Kompression.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    class TaikoLz81Encoder : IPatternMatchEncoder
    {
        private static int[] _counters =
        {
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 0xa, 0xc, 0xe,
            0x10, 0x12, 0x16, 0x1a,
            0x1e, 0x22, 0x2a, 0x32,
            0x3a, 0x42, 0x52, 0x62,
            0x72, 0x82, 0xa2, 0xc2,
            0xe2, 0x102, 0, 0
        };

        private static int[] _counterBitReads =
        {
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 1, 1, 1,
            1, 2, 2, 2,
            2, 3, 3, 3,
            3, 4, 4, 4,
            4, 5, 5, 5,
            5, 0, 0, 0
        };

        private static int[] _dispRanges =
        {
            1, 2, 3, 4,
            5, 7, 9, 0xd,
            0x11, 0x19, 0x21, 0x31,
            0x41, 0x61, 0x81, 0xc1,
            0x101, 0x181, 0x201, 0x301,
            0x401, 0x601, 0x801, 0xc01,
            0x1001, 0x1801, 0x2001, 0x3001,
            0x4001, 0x6001, 0, 0
        };

        private static int[] _dispBitReads =
        {
            0, 0, 0, 0,
            1, 1, 2, 2,
            3, 3, 4, 4,
            5, 5, 6, 6,
            7, 7, 8, 8,
            9, 9, 0xa, 0xa,
            0xb, 0xb, 0xc, 0xc,
            0xd, 0xd, 0, 0
        };

        private byte[] _countIndexes;
        private byte[] _dispIndexes;
        private Dictionary<int, string> _rawValueDictionary;
        private Dictionary<int, string> _countIndexDictionary;
        private Dictionary<int, string> _dispIndexDictionary;

        public void Encode(Stream input, Stream output, Match[] matches)
        {
            var rawValueTree = CreateRawValueTree(input, matches);
            _rawValueDictionary = rawValueTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);

            _countIndexes = GetCountIndexes(matches, input.Length);
            var countIndexValueTree = CreateIndexValueTree();
            _countIndexDictionary = countIndexValueTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);

            _dispIndexes = GetDispIndexes(matches);
            var dispIndexTree = CreateDispIndexTree();
            _dispIndexDictionary = dispIndexTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);

            using (var bw = new BitWriter(output, BitOrder.LSBFirst, 1, ByteOrder.LittleEndian))
            {
                // Without obfuscation
                bw.WriteByte(0x02);

                WriteTreeNode(bw, rawValueTree, 8);
                WriteTreeNode(bw, countIndexValueTree, 6);
                WriteTreeNode(bw, dispIndexTree, 5);

                var countPosition = 0;
                var displacementPosition = 0;
                foreach (var match in matches)
                {
                    // Compress raw data
                    if (input.Position < match.Position)
                        CompressRawData(input, bw, (int)(match.Position - input.Position), ref countPosition);

                    // Compress match
                    CompressMatchData(input, bw, match, ref countPosition, ref displacementPosition);
                }

                // Compress raw data
                if (input.Position < input.Length)
                    CompressRawData(input, bw, (int)(input.Length - input.Position), ref countPosition);

                // Write final 0 index
                foreach (var bit in _countIndexDictionary[_countIndexes.Last()])
                    bw.WriteBit(bit - '0');
            }
        }

        #region Tree creation

        private HuffmanTreeNode CreateRawValueTree(Stream input, Match[] matches)
        {
            var inputArray = ToArray(input);
            var huffmanInput = RemoveMatchesFromInput(inputArray, matches);
            var tree = new HuffmanTree(8);
            return tree.Build(inputArray);
        }

        private HuffmanTreeNode CreateIndexValueTree()
        {
            var tree = new HuffmanTree(8);
            return tree.Build(_countIndexes);
        }

        private HuffmanTreeNode CreateDispIndexTree()
        {
            var tree = new HuffmanTree(8);
            return tree.Build(_dispIndexes);
        }

        private byte[] RemoveMatchesFromInput(byte[] input, Match[] matches)
        {
            var huffmanInput = new byte[input.Length - matches.Sum(x => x.Length)];

            var huffmanInputPosition = 0;
            var inputArrayPosition = 0;
            foreach (var match in matches)
            {
                for (var i = inputArrayPosition; i < match.Position; i++)
                    huffmanInput[huffmanInputPosition++] = input[i];

                inputArrayPosition += (int)match.Position - inputArrayPosition;
                inputArrayPosition += (int)match.Length;
            }

            for (var i = inputArrayPosition; i < input.Length; i++)
                huffmanInput[huffmanInputPosition++] = input[i];

            return huffmanInput;
        }

        /// <summary>
        /// Converts a stream to an array.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <returns>The converted array.</returns>
        private byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length];

            input.Read(inputArray, 0, inputArray.Length);

            input.Position = bkPos;
            return inputArray;
        }

        #endregion

        #region Get indexes

        private byte[] GetCountIndexes(Match[] matches, long inputLength)
        {
            var result = new List<byte>();

            long position = 0;
            foreach (var match in matches)
            {
                if (position < match.Position)
                {
                    var rawLength = match.Position - position;
                    while (rawLength > 0)
                    {
                        var cappedLength = Math.Min(rawLength, 0x102);
                        rawLength -= cappedLength;
                        result.Add((byte)(GetCountIndex(cappedLength) + 0x20));
                    }
                    position = match.Position;
                }

                result.Add(GetCountIndex(match.Length));
                position += match.Length;
            }

            if (position < inputLength)
            {
                var rawLength = inputLength - position;
                while (rawLength > 0)
                {
                    var cappedLength = Math.Min(rawLength, 0x102);
                    rawLength -= cappedLength;
                    result.Add((byte)(GetCountIndex(cappedLength) + 0x20));
                }
            }

            result.Add(0);
            return result.ToArray();
        }

        private byte GetCountIndex(long length)
        {
            if (length == _counters[0x1D])
                return 0x1D;

            for (byte i = 0; i < 0x1D; i++)
            {
                if (length >= _counters[i] && length < _counters[i + 1])
                    return i;
            }

            return 0xFF;
        }

        private byte[] GetDispIndexes(Match[] matches)
        {
            var result = new List<byte>();

            foreach (var match in matches)
            {
                result.Add(GetDispIndex(match.Displacement));
            }

            return result.ToArray();
        }

        private byte GetDispIndex(long displacement)
        {
            if (displacement >= _dispRanges[0x1D])
                return 0x1D;

            for (byte i = 0; i < 0x1D; i++)
            {
                if (displacement >= _dispRanges[i] && displacement < _dispRanges[i + 1])
                    return i;
            }

            return 0xFF;
        }

        #endregion

        private void WriteTreeNode(BitWriter bw, HuffmanTreeNode huffmanTreeNode, int bitCount)
        {
            if (huffmanTreeNode.IsLeaf)
            {
                bw.WriteBit(0);
                bw.WriteBits(huffmanTreeNode.Code, bitCount);
                return;
            }

            bw.WriteBit(1);
            WriteTreeNode(bw, huffmanTreeNode.Children[0], bitCount);
            WriteTreeNode(bw, huffmanTreeNode.Children[1], bitCount);
        }

        private void CompressRawData(Stream input, BitWriter bw, int rawLength, ref int countPosition)
        {
            while (rawLength > 0)
            {
                var cappedLength = Math.Min(rawLength, 0x102);
                rawLength -= cappedLength;

                // Write the index to the counters table
                var countIndex = _countIndexes[countPosition++];
                foreach (var bit in _countIndexDictionary[countIndex])
                    bw.WriteBit(bit - '0');

                // Write additional bits to reach intermediate lengths
                if (_counterBitReads[countIndex - 0x20] > 0)
                    bw.WriteBits(cappedLength - _counters[countIndex - 0x20], _counterBitReads[countIndex - 0x20]);

                // Write values
                for (int i = 0; i < cappedLength; i++)
                    foreach (var bit in _rawValueDictionary[input.ReadByte()])
                        bw.WriteBit(bit - '0');
            }
        }

        private void CompressMatchData(Stream input, BitWriter bw, Match match, ref int countPosition, ref int displacementPosition)
        {
            // Write the index to the counters table
            var countIndex = _countIndexes[countPosition++];
            foreach (var bit in _countIndexDictionary[countIndex])
                bw.WriteBit(bit - '0');

            // Write additional bits to reach intermediate lengths
            if (_counterBitReads[countIndex] > 0)
                bw.WriteBits((int)match.Length - _counters[countIndex], _counterBitReads[countIndex]);

            // Write the index to the displacement table
            var displacementIndex = _dispIndexes[displacementPosition++];
            foreach (var bit in _dispIndexDictionary[displacementIndex])
                bw.WriteBit(bit - '0');

            // Write additional bits to reach intermediate displacements
            if (_dispBitReads[displacementIndex] > 0)
                bw.WriteBits((int)match.Displacement - _dispRanges[displacementIndex], _dispBitReads[displacementIndex]);

            input.Position += match.Length;
        }

        public void Dispose()
        {
            _countIndexes = null;
            _dispIndexes = null;
            _rawValueDictionary = null;
            _countIndexDictionary = null;
            _dispIndexDictionary = null;
        }
    }
}
