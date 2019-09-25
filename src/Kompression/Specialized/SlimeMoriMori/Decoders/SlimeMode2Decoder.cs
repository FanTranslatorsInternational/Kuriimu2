using System.Diagnostics;
using System.IO;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class SlimeMode2Decoder : SlimeDecoder
    {
        public SlimeMode2Decoder(IValueReader huffmanReader) : base(huffmanReader)
        {
        }

        public override void Decode(Stream input, Stream output)
        {
            using (var br = new BitReader(input, BitOrder.MSBFirst, 4, ByteOrder.LittleEndian))
            {
                var uncompressedSize = br.ReadInt32() >> 8;
                br.ReadByte();
                HuffmanReader.BuildTree(br);
                SetupDisplacementTable(br, 7);

                while (output.Length < uncompressedSize)
                {
                    if (br.ReadBit() == 1)
                    {
                        var matchLength = 0;
                        int displacement;

                        var dispIndex = br.ReadBits<byte>(3);
                        if (dispIndex == 7)
                        {
                            // 8098E4C
                            byte readValue;
                            // Add lengthBitCount bit values as long as read values' LSB is set
                            // Seems to be a variable length value
                            do
                            {
                                readValue = br.ReadBits<byte>(4);
                                matchLength = (matchLength << 3) | (readValue >> 1);
                            } while ((readValue & 0x1) == 1);

                            if (br.ReadBit() == 1)
                            {
                                // 8098E64
                                dispIndex = br.ReadBits<byte>(3);
                                displacement = GetDisplacement(br, dispIndex);

                                matchLength = ((matchLength << 4) | br.ReadBits<byte>(4)) + 3;
                                // Goto 8098EA2
                            }
                            else
                            {
                                // 8098E88
                                matchLength++;
                                ReadHuffmanValues(br, output, matchLength, 1);

                                continue;
                            }
                        }
                        else
                        {
                            // 8098E32
                            displacement = GetDisplacement(br, dispIndex);

                            matchLength = br.ReadBits<byte>(4) + 3;
                            // Goto 8098EA2
                        }

                        // Label 8098EA2
                        ReadDisplacement(output, displacement, matchLength, 1);
                    }
                    else
                    {
                        // 8098E14
                        ReadHuffmanValues(br, output, 1, 1);
                    }
                }
            }
        }
    }
}
