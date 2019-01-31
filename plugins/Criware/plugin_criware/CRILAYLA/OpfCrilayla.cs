using Komponent.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_criware.CRILAYLA
{
    [Endianness(ByteOrder = ByteOrder.LittleEndian)]
    public class OpfCrilaylaHeader
    {
        [FixedLength(8)]
        public string Magic = "CRILAYLA";
        public int UncompressedSize;
        public int CompressedSize;
    }

    public class OpfCrilayla
    {
        public static byte[] Decompress(Stream src)
        {
            byte[] dest;

            using (var br = new BinaryReaderX(src, true, ByteOrder.BigEndian, BitOrder.LSBFirst, 4))
            {
                var header = br.ReadStruct<OpfCrilaylaHeader>();

                if (header.Magic != "CRILAYLA" || header.Magic == "\0\0\0\0\0\0\0\0")
                    throw new InvalidOperationException();

                var lengthNeeded = 0x100 + header.UncompressedSize;
                dest = new byte[lengthNeeded];

                br.BaseStream.Position = br.BaseStream.Length - 0x100;
                var uncompressedData = br.ReadBytes(0x100);
                uncompressedData.CopyTo(dest, 0);
            }

            using (var br = new BinaryReaderX(new ReverseStream(src), ByteOrder.LittleEndian, BitOrder.MSBFirst, 4))
            {
                // Actual decompression inits
                br.BaseStream.Position = br.BaseStream.Length - 0x100;
                var destPos = dest.Length;

                void backward_cascading_copy(int src_index, int num_bytes)
                {
                    var d = destPos + num_bytes;
                    var s = src_index + num_bytes;

                    while (num_bytes-- > 0)
                        dest[--d] = dest[--s];
                }

                // Actual decompression
                while (destPos > 0x100)
                {
                    if (br.ReadBit())
                    {
                        var initialOffset = br.ReadBits<short>(13);
                        var offset = destPos + initialOffset;
                        destPos -= 3;

                        int ReadOffset(int bits) { var value = br.ReadBits<int>(bits); destPos -= value; offset -= value; return value; };

                        int more;
                        if (initialOffset >= 3)
                        {
                            // No overlap between the two possible, combine.
                            more = ReadOffset(2);
                            Array.Copy(dest, offset, dest, destPos, 3 + more);
                        }
                        else
                        {
                            // Two copies, for overlap (but internal overlap of these two is not possible.)
                            Array.Copy(dest, offset, dest, destPos, 3);
                            more = ReadOffset(2);
                            Array.Copy(dest, offset, dest, destPos, more);
                        }

                        if (more == 3)
                        {
                            more = ReadOffset(3);
                            // Note any value > 3 could cause a cascade for the below.
                            backward_cascading_copy(offset, more);

                            if (more == 7)
                            {
                                more = ReadOffset(5);
                                backward_cascading_copy(offset, more);

                                if (more == 31)
                                {
                                    do
                                    {
                                        more = ReadOffset(8);
                                        backward_cascading_copy(offset, more);
                                    }
                                    while (more == 255);
                                }
                            }
                        }
                    }
                    else
                    {
                        dest[--destPos] = br.ReadBits<byte>(8);
                    }
                }
            }

            return dest;
        }
    }
}
