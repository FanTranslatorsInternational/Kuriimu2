using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.InternalContract.SlimeMoriMori.ValueReader;

namespace Kompression.Specialized.SlimeMoriMori.Decoder
{
    internal class SlimeMode1Decoder(IValueReader huffmanReader) : SlimeDecoder(huffmanReader)
    {
        public override void Decode(Stream input, Stream output)
        {
            using var br = new BinaryBitReader(input, BitOrder.MostSignificantBitFirst, 4, ByteOrder.LittleEndian);

            var uncompressedSize = br.ReadInt32() >> 8;
            br.ReadByte();
            HuffmanReader.BuildTree(br);
            SetupDisplacementTable(br, 4);

            while (output.Length < uncompressedSize)
            {
                if (br.ReadBit() == 1)
                {
                    var displacement = GetDisplacement(br, br.ReadBits<byte>(2));
                    var matchLength = br.ReadBits<byte>(4) + 3;

                    ReadDisplacement(output, displacement, matchLength, 1);
                }
                else
                {
                    ReadHuffmanValues(br, output, 1, 1);
                }
            }
        }
    }
}
