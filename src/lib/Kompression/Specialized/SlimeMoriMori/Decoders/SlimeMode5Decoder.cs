using System.IO;
using Komponent.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;
using Kontract.Models.IO;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class SlimeMode5Decoder : SlimeDecoder
    {
        public SlimeMode5Decoder(IValueReader huffmanReader) : base(huffmanReader)
        {
        }

        public override void Decode(Stream input, Stream output)
        {
            using (var br = new BitReader(input, BitOrder.MostSignificantBitFirst, 4, ByteOrder.LittleEndian))
            {
                var uncompressedSize = br.ReadInt32() >> 8;
                br.ReadByte();
                HuffmanReader.BuildTree(br);
                SetupDisplacementTable(br, 2);

                while (output.Length < uncompressedSize)
                {
                    int matchLength;
                    int displacement;

                    var value0 = br.ReadBits<byte>(2);
                    if (value0 < 2) // cmp value0, #2 -> bcc
                    {
                        // CC4
                        displacement = GetDisplacement(br, value0);
                        matchLength = br.ReadBits<byte>(6) + 3;

                        // Goto EC4
                    }
                    else
                    {
                        if (value0 == 2)
                        {
                            // CDC
                            matchLength = br.ReadBits<byte>(6) + 1;
                            ReadHuffmanValues(br, output, matchLength, 1);

                            continue;
                        }

                        // value0 == 3
                        // CA4
                        matchLength = br.ReadBits<byte>(6) + 1;
                        displacement = 1;
                        output.WriteByte(br.ReadBits<byte>(8)); // read static 8 bit; we don't read a huffman value here

                        // Goto EC4
                    }

                    // EC4
                    ReadDisplacement(output, displacement, matchLength, 1);
                }
            }
        }
    }
}
