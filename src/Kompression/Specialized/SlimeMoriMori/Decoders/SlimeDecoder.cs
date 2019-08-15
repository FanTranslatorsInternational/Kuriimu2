using System.IO;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.Huffman;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    abstract class SlimeDecoder : ISlimeDecoder
    {
        private DisplacementElement[] _displacementTable;

        protected readonly ISlimeHuffmanReader HuffmanReader;

        protected SlimeDecoder(ISlimeHuffmanReader huffmanReader)
        {
            HuffmanReader = huffmanReader;
        }

        public abstract void Decode(Stream input, Stream output);

        protected void ReadHuffmanValues(BitReader br, Stream output, int count, int bytesToRead)
        {
            for (var i = 0; i < count; i++)
                for (var j = 0; j < bytesToRead; j++)
                    output.WriteByte(HuffmanReader.ReadValue(br));
        }

        protected void SetupDisplacementTable(BitReader br, int displacementTableCount)
        {
            _displacementTable = new DisplacementElement[displacementTableCount];
            for (var i = 0; i < displacementTableCount; i++)
            {
                if (i == 0)
                    _displacementTable[0] = new DisplacementElement((byte)(br.ReadBits<int>(4) + 1), 1);
                else
                {
                    var newDisplacementStart = (1 << _displacementTable[i - 1].ReadBits) + _displacementTable[i - 1].DisplacementStart;
                    _displacementTable[i] = new DisplacementElement((byte)(br.ReadBits<int>(4) + 1), (short)newDisplacementStart);
                }
            }
        }

        protected int GetDisplacement(BitReader br, int dispIndex)
        {
            return br.ReadBits<int>(_displacementTable[dispIndex].ReadBits) +
                   _displacementTable[dispIndex].DisplacementStart;
        }

        protected void ReadDisplacement(Stream output, int displacement, int matchLength, int bytesToRead)
        {
            for (var i = 0; i < matchLength; i++)
            {
                var position = output.Position;
                for (var j = 0; j < bytesToRead; j++)
                {
                    output.Position = position - displacement;
                    var matchValue = (byte)output.ReadByte();

                    output.Position = position;
                    output.WriteByte(matchValue);

                    position++;
                }
            }
        }
    }
}
