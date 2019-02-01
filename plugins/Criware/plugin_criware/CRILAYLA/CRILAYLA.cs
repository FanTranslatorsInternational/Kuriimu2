using System;
using System.IO;
using Komponent.IO;

namespace plugin_criware.CRILAYLA
{
    /// <summary>
    /// The basic CRILAYLA header.
    /// </summary>
    [Endianness(ByteOrder = ByteOrder.LittleEndian)]
    public class CrilaylaHeader
    {
        [FixedLength(8)]
        public string Magic = "CRILAYLA";
        public int UncompressedSize;
        public int CompressedSize;
    }

    /// <summary>
    /// CRILAYLA algorithm for compression and decompression.
    /// </summary>
    public class CRILAYLA
    {
        private const int HeaderLength = 0x10;
        private const int UncompressedDataLength = 0x100;

        /// <summary>
        /// Compress a file using the CRILAYLA compression.
        /// </summary>
        /// <param name="input">Uncompressed file.</param>
        /// <returns></returns>
        public static byte[] Compress(Stream input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decompress a CRILAYLA compressed file.
        /// </summary>
        /// <param name="input">Compressed file.</param>
        /// <returns></returns>
        public static byte[] Decompress(Stream input)
        {
            byte[] dest;

            // Reset incoming stream
            input.Position = 0;

            using (var br = new BinaryReaderX(input, true, ByteOrder.BigEndian, BitOrder.LSBFirst))
            {
                var header = br.ReadStruct<CrilaylaHeader>();

                if (header.Magic != "CRILAYLA" || header.Magic == "\0\0\0\0\0\0\0\0")
                    throw new InvalidOperationException();

                var lengthNeeded = UncompressedDataLength + header.UncompressedSize;
                dest = new byte[lengthNeeded];

                br.BaseStream.Position = br.BaseStream.Length - UncompressedDataLength;
                br.ReadBytes(UncompressedDataLength).CopyTo(dest, 0);
            }

            using (var br = new BinaryReaderX(new ReverseStream(input), true))
            {
                // Actual decompression inits
                br.BaseStream.Position = br.BaseStream.Length - UncompressedDataLength;
                var destPos = dest.Length;

                void BackwardCascadingCopy(int sourceIndex, int count)
                {
                    var d = destPos + count;
                    var s = sourceIndex + count;

                    while (count-- > 0)
                        dest[--d] = dest[--s];
                }

                // Actual decompression
                while (destPos > UncompressedDataLength)
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
                            BackwardCascadingCopy(offset, more);

                            if (more == 7)
                            {
                                more = ReadOffset(5);
                                BackwardCascadingCopy(offset, more);

                                if (more == 31)
                                {
                                    do
                                    {
                                        more = ReadOffset(8);
                                        BackwardCascadingCopy(offset, more);
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
