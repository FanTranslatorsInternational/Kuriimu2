using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.Huffman;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Contract.Encoder.Huffman;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Encoder.LempelZiv.PriceCalculators;
using Kompression.Extensions;

namespace Kompression.Encoder
{
    public class TaikoLz81Encoder : ILempelZivHuffmanEncoder
    {
        private static readonly int[] Counters =
        [
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 0xa, 0xc, 0xe,
            0x10, 0x12, 0x16, 0x1a,
            0x1e, 0x22, 0x2a, 0x32,
            0x3a, 0x42, 0x52, 0x62,
            0x72, 0x82, 0xa2, 0xc2,
            0xe2, 0x102, 0, 0
        ];

        private static readonly int[] CounterBitReads =
        [
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 1, 1, 1,
            1, 2, 2, 2,
            2, 3, 3, 3,
            3, 4, 4, 4,
            4, 5, 5, 5,
            5, 0, 0, 0
        ];

        private static readonly int[] DispRanges =
        [
            1, 2, 3, 4,
            5, 7, 9, 0xd,
            0x11, 0x19, 0x21, 0x31,
            0x41, 0x61, 0x81, 0xc1,
            0x101, 0x181, 0x201, 0x301,
            0x401, 0x601, 0x801, 0xc01,
            0x1001, 0x1801, 0x2001, 0x3001,
            0x4001, 0x6001, 0, 0
        ];

        private static readonly int[] DispBitReads =
        [
            0, 0, 0, 0,
            1, 1, 2, 2,
            3, 3, 4, 4,
            5, 5, 6, 6,
            7, 7, 8, 8,
            9, 9, 0xa, 0xa,
            0xb, 0xb, 0xc, 0xc,
            0xd, 0xd, 0, 0
        ];

        internal class Block
        {
            public required byte[] CountIndexes;
            public required byte[] DispIndexes;
            public required Dictionary<int, string> RawValueDictionary;
            public required Dictionary<int, string> CountIndexDictionary;
            public required Dictionary<int, string> DispIndexDictionary;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions, IHuffmanEncoderOptionsBuilder huffmanOptions)
        {
            matchOptions.CalculatePricesWith(() => new TaikoLz81PriceCalculator())
                .FindPatternMatches().WithinLimitations(1, 0x102, 2, 0x8000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches, IHuffmanTreeBuilder treeBuilder)
        {
            var matchArray = matches.ToArray();

            var rawValueTree = CreateRawValueTree(input, matchArray, treeBuilder);
            ArgumentNullException.ThrowIfNull(rawValueTree);

            var countIndexes = GetCountIndexes(matchArray, input.Length);
            var countIndexValueTree = CreateIndexValueTree(countIndexes, treeBuilder);
            ArgumentNullException.ThrowIfNull(countIndexValueTree);

            var dispIndexes = GetDispIndexes(matchArray);
            var dispIndexTree = CreateDisplacementIndexTree(dispIndexes, treeBuilder);
            ArgumentNullException.ThrowIfNull(dispIndexTree);

            var block = new Block
            {
                CountIndexes = countIndexes,
                DispIndexes = dispIndexes,
                RawValueDictionary = rawValueTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2),
                CountIndexDictionary = countIndexValueTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2),
                DispIndexDictionary = dispIndexTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2)
            };

            using var bw = new BinaryBitWriter(output, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.LittleEndian);

            // Without obfuscation
            bw.WriteByte(0x02);

            WriteTreeNode(bw, rawValueTree, 8);
            WriteTreeNode(bw, countIndexValueTree, 6);
            WriteTreeNode(bw, dispIndexTree, 5);

            var countPosition = 0;
            var displacementPosition = 0;
            foreach (var match in matchArray)
            {
                // Compress raw data
                if (input.Position < match.Position)
                    CompressRawData(input, bw, block, (int)(match.Position - input.Position), ref countPosition);

                // Compress match
                CompressMatchData(input, bw, block, match, ref countPosition, ref displacementPosition);
            }

            // Compress raw data
            if (input.Position < input.Length)
                CompressRawData(input, bw, block, (int)(input.Length - input.Position), ref countPosition);

            // Write final 0 index
            foreach (var bit in block.CountIndexDictionary[block.CountIndexes.Last()])
                bw.WriteBit(bit - '0');
        }

        #region Tree creation

        private static HuffmanTreeNode? CreateRawValueTree(Stream input, LempelZivMatch[] matches, IHuffmanTreeBuilder treeBuilder)
        {
            var huffmanInput = RemoveMatchesFromInput(input.ToArray(), matches);
            return treeBuilder.Build(huffmanInput, 8, NibbleOrder.LowNibbleFirst);
        }

        private static HuffmanTreeNode? CreateIndexValueTree(byte[] countIndexes, IHuffmanTreeBuilder treeBuilder)
        {
            return treeBuilder.Build(countIndexes, 8, NibbleOrder.LowNibbleFirst);
        }

        private static HuffmanTreeNode? CreateDisplacementIndexTree(byte[] dispIndexes, IHuffmanTreeBuilder treeBuilder)
        {
            return treeBuilder.Build(dispIndexes, 8, NibbleOrder.LowNibbleFirst);
        }

        private static byte[] RemoveMatchesFromInput(byte[] input, LempelZivMatch[] matches)
        {
            var huffmanInput = new byte[input.Length - matches.Sum(x => x.Length)];

            var huffmanInputPosition = 0;
            var inputArrayPosition = 0;
            foreach (var match in matches)
            {
                for (var i = inputArrayPosition; i < match.Position; i++)
                    huffmanInput[huffmanInputPosition++] = input[i];

                inputArrayPosition += match.Position - inputArrayPosition;
                inputArrayPosition += match.Length;
            }

            for (var i = inputArrayPosition; i < input.Length; i++)
                huffmanInput[huffmanInputPosition++] = input[i];

            return huffmanInput;
        }

        #endregion

        #region Get indexes

        private static byte[] GetCountIndexes(LempelZivMatch[] matches, long inputLength)
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
            return [.. result];
        }

        private static byte GetCountIndex(long length)
        {
            if (length == Counters[0x1D])
                return 0x1D;

            for (byte i = 0; i < 0x1D; i++)
            {
                if (length >= Counters[i] && length < Counters[i + 1])
                    return i;
            }

            return 0xFF;
        }

        private static byte[] GetDispIndexes(LempelZivMatch[] matches)
        {
            var result = new List<byte>();

            foreach (var match in matches)
            {
                result.Add(GetDispIndex(match.Displacement));
            }

            return [.. result];
        }

        private static byte GetDispIndex(long displacement)
        {
            if (displacement >= DispRanges[0x1D])
                return 0x1D;

            for (byte i = 0; i < 0x1D; i++)
            {
                if (displacement >= DispRanges[i] && displacement < DispRanges[i + 1])
                    return i;
            }

            return 0xFF;
        }

        #endregion

        private static void WriteTreeNode(BinaryBitWriter bw, HuffmanTreeNode huffmanTreeNode, int bitCount)
        {
            if (huffmanTreeNode.IsLeaf)
            {
                bw.WriteBit(0);
                bw.WriteBits(huffmanTreeNode.Code, bitCount);
                return;
            }

            bw.WriteBit(1);
            WriteTreeNode(bw, huffmanTreeNode.Children![0], bitCount);
            WriteTreeNode(bw, huffmanTreeNode.Children![1], bitCount);
        }

        private static void CompressRawData(Stream input, BinaryBitWriter bw, Block block, int rawLength, ref int countPosition)
        {
            while (rawLength > 0)
            {
                var cappedLength = Math.Min(rawLength, 0x102);
                rawLength -= cappedLength;

                // Write the index to the counters table
                var countIndex = block.CountIndexes[countPosition++];
                foreach (var bit in block.CountIndexDictionary[countIndex])
                    bw.WriteBit(bit - '0');

                // Write additional bits to reach intermediate lengths
                if (CounterBitReads[countIndex - 0x20] > 0)
                    bw.WriteBits(cappedLength - Counters[countIndex - 0x20], CounterBitReads[countIndex - 0x20]);

                // Write values
                for (int i = 0; i < cappedLength; i++)
                    foreach (var bit in block.RawValueDictionary[input.ReadByte()])
                        bw.WriteBit(bit - '0');
            }
        }

        private static void CompressMatchData(Stream input, BinaryBitWriter bw, Block block, LempelZivMatch lempelZivMatch, ref int countPosition, ref int displacementPosition)
        {
            // Write the index to the counters table
            var countIndex = block.CountIndexes[countPosition++];
            foreach (var bit in block.CountIndexDictionary[countIndex])
                bw.WriteBit(bit - '0');

            // Write additional bits to reach intermediate lengths
            if (CounterBitReads[countIndex] > 0)
                bw.WriteBits(lempelZivMatch.Length - Counters[countIndex], CounterBitReads[countIndex]);

            // Write the index to the displacement table
            var displacementIndex = block.DispIndexes[displacementPosition++];
            foreach (var bit in block.DispIndexDictionary[displacementIndex])
                bw.WriteBit(bit - '0');

            // Write additional bits to reach intermediate displacements
            if (DispBitReads[displacementIndex] > 0)
                bw.WriteBits(lempelZivMatch.Displacement - DispRanges[displacementIndex], DispBitReads[displacementIndex]);

            input.Position += lempelZivMatch.Length;
        }
    }
}
