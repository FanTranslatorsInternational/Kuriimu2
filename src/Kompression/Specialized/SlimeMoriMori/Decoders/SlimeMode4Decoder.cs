using System.IO;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class SlimeMode4Decoder : SlimeDecoder
    {
        public SlimeMode4Decoder(IValueReader huffmanReader) : base(huffmanReader)
        {
        }

        public override void Decode(Stream input, Stream output)
        {
            using (var br = new BitReader(input, BitOrder.MSBFirst, 4, ByteOrder.LittleEndian))
            {
                var uncompressedSize = br.ReadInt32() >> 8;
                br.ReadByte();
                HuffmanReader.BuildTree(br);

                while (output.Length < uncompressedSize)
                    ReadHuffmanValues(br, output, 1, 1);
            }
        }
    }
}
