using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.Huffman
{
    class HuffmanTree
    {
        private readonly int _bitDepth;
        private readonly byte[] _table;

        public HuffmanTree(int bitDepth)
        {
            _bitDepth = bitDepth;
            _table = new byte[(1 << bitDepth) * 4];
            for (var i = 0; i < (1 << bitDepth) * 4; i++)
                _table[i] = 0xFF;
        }

        public void Build(BitReader br)
        {
            var tableIndex2 = 0;
            var counter3 = 0;
            var counter2 = 0;
            var counter = 16;
            do
            {
                counter3 <<= 1;
                var value = br.ReadBits<byte>(8);
                var valueBk = value;

                if (value != 0)
                {
                    do
                    {
                        var tableIndex = 0;
                        var internalCounter = counter2;

                        while (internalCounter != 0)
                        {
                            var newTableIndex = ((counter3 >> internalCounter) & 0x1) * 2 + tableIndex;
                            tableIndex = (short)(_table[newTableIndex] | (_table[newTableIndex + 1] << 8));
                            if (tableIndex < 0)
                            {
                                tableIndex = tableIndex2 + 4;

                                // Reference to another node
                                _table[newTableIndex] = (byte)tableIndex;
                                _table[newTableIndex + 1] = (byte)(tableIndex >> 8);
                                tableIndex2 = tableIndex;
                            }

                            internalCounter--;
                        }

                        value = br.ReadBits<byte>(_bitDepth);

                        // Value in tree
                        _table[(counter3 & 0x1) * 2 + tableIndex] = (byte)~value;
                        _table[(counter3 & 0x1) * 2 + tableIndex + 1] = (byte)((~value) >> 8);

                        counter3++;
                        valueBk--;
                    } while (valueBk != 0);
                }

                counter2++;
                counter--;
            } while (counter != 0);
        }

        public byte GetValue(BitReader br)
        {
            short tableValue = 0;

            do
            {
                var tableIndex = (br.ReadBits<byte>(1) << 1) + tableValue;
                tableValue = (short)(_table[tableIndex] | (_table[tableIndex + 1] << 8));
            } while (tableValue >= 0);

            return (byte)~tableValue;
        }
    }
}
