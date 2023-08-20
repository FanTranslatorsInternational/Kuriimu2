using System.IO;
using Komponent.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    abstract class SlimeDecoder : ISlimeDecoder
    {
        private DisplacementElement[] _displacementTable;

        protected readonly IValueReader HuffmanReader;

        protected SlimeDecoder(IValueReader huffmanReader)
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
                    //if (position - displacement < 0)
                    //    Debugger.Break();
                    //if(position>=0x1ac)
                    //    Debugger.Break();

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
