using Komponent.IO;
using Kompression.DataClasses.SlimeMoriMori.Decoder;
using Kompression.InternalContract.SlimeMoriMori.Decoder;
using Kompression.InternalContract.SlimeMoriMori.ValueReader;

namespace Kompression.Specialized.SlimeMoriMori.Decoder
{
    abstract class SlimeDecoder(IValueReader huffmanReader) : ISlimeDecoder
    {
        private DisplacementElement[]? _displacementTable;

        protected readonly IValueReader HuffmanReader = huffmanReader;

        public abstract void Decode(Stream input, Stream output);

        protected void ReadHuffmanValues(BinaryBitReader br, Stream output, int count, int bytesToRead)
        {
            for (var i = 0; i < count; i++)
                for (var j = 0; j < bytesToRead; j++)
                    output.WriteByte(HuffmanReader.ReadValue(br));
        }

        protected void SetupDisplacementTable(BinaryBitReader br, int displacementTableCount)
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

        protected int GetDisplacement(BinaryBitReader br, int dispIndex)
        {
            if (_displacementTable is null)
                throw new InvalidOperationException("Did not set up displacement table correctly.");

            return br.ReadBits<int>(_displacementTable[dispIndex].ReadBits) +
                   _displacementTable[dispIndex].DisplacementStart;
        }

        protected static void ReadDisplacement(Stream output, int displacement, int matchLength, int bytesToRead)
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
