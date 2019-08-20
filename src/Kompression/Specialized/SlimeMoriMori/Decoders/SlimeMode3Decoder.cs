using System.IO;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueReaders;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class SlimeMode3Decoder : SlimeDecoder
    {
        public SlimeMode3Decoder(IValueReader huffmanReader) : base(huffmanReader)
        {
        }

        public override void Decode(Stream input, Stream output)
        {
            using (var br = new BitReader(input, BitOrder.MSBFirst, 4, ByteOrder.LittleEndian))
            {
                var uncompressedSize = br.ReadInt32() >> 8;
                br.ReadByte();
                HuffmanReader.BuildTree(br);
                SetupDisplacementTable(br, 3);

                while (output.Length < uncompressedSize)
                {
                    if (br.ReadBit() == 1)
                    {
                        var matchLength = 0;
                        int displacement;

                        var dispIndex = br.ReadBits<byte>(2);
                        if (dispIndex == 3)
                        {
                            // 8098E4C
                            byte readValue;
                            // Add lengthBitCount bit values as long as read values' LSB is set
                            // Seems to be a variable length value
                            do
                            {
                                readValue = br.ReadBits<byte>(3);
                                matchLength = (matchLength << 2) | (readValue >> 1);
                            } while ((readValue & 0x1) == 1);

                            if (br.ReadBit() == 1)
                            {
                                // 8098E64
                                dispIndex = br.ReadBits<byte>(2);
                                displacement = GetDisplacement(br, dispIndex) << 1;

                                matchLength = ((matchLength << 3) | br.ReadBits<byte>(3)) + 2;
                                // Goto 8098EA2
                            }
                            else
                            {
                                // 8098E88
                                matchLength++;
                                ReadHuffmanValues(br, output, matchLength, 2);

                                continue;
                            }
                        }
                        else
                        {
                            // 8098E32
                            displacement = GetDisplacement(br, dispIndex) << 1;

                            matchLength = br.ReadBits<byte>(3) + 2;
                            // Goto 8098EA2
                        }

                        // Label 8098EA2
                        ReadDisplacement(output, displacement, matchLength, 2);
                    }
                    else
                    {
                        // 8098E14
                        ReadHuffmanValues(br, output, 1, 2);
                    }
                }
            }
        }
    }
}
