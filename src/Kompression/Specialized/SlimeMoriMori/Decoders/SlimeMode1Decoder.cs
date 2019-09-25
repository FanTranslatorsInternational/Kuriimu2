using System.IO;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class SlimeMode1Decoder : SlimeDecoder
    {
        public SlimeMode1Decoder(IValueReader huffmanReader) : base(huffmanReader)
        {
        }

        public override void Decode(Stream input, Stream output)
        {
            using (var br = new BitReader(input, BitOrder.MSBFirst, 4, ByteOrder.LittleEndian))
            {
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
}
