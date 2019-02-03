using System;
using System.IO;
using Komponent.IO;
using System.Linq;

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

        private static (byte[] backref, int backrefPos) LongestMatch(int needle_len, byte[] haystack)
        {
            byte[] needle = new byte[needle_len];
            Array.Copy(haystack, 0, needle, 0, needle_len);
            byte[] longest_so_far = null;
            int longest_so_far_pos = 0;

            void ConsiderPossibleMatch(int starting_index)
            {
                var haystack_index = starting_index;
                for (int j = needle.Length - 2; j >= 0; j--)
                {
                    if (haystack[--haystack_index] != needle[j])
                        break;
                }

                var len = starting_index - haystack_index;

                if (len > (longest_so_far?.Length ?? 0))
                {
                    longest_so_far = new byte[len];
                    Array.Copy(haystack, haystack_index + 1, longest_so_far, 0, len);
                    longest_so_far_pos = haystack_index + 1;
                }
            }

            byte starting_byte = needle[needle.Length - 1];

            // Three bytes including the match, so 2 ahead.
            int minimum = needle_len + 2;
            int i = minimum;
            while (i < haystack.Length)
            {
                var index = Array.IndexOf(haystack, starting_byte, i, haystack.Length - i);
                if (index < 0)
                    break;

                i = index;
                ConsiderPossibleMatch(i);
                ++i;
            }

            return (longest_so_far, longest_so_far_pos);
        }

        /// <summary>
        /// Compress a file using the CRILAYLA compression.
        /// </summary>
        /// <param name="input">Uncompressed file.</param>
        /// <returns></returns>
        public static byte[] Compress(Stream input)
        {
            if (input.Length <= 0x100)
                throw new ArgumentException("Input needs to be longer than 256 bytes");

            byte[] uncompressedData;
            using (var br = new BinaryReaderX(input))
            {
                uncompressedData = br.ReadBytes(0x100);
                var inputSize = (int)input.Length - 0x100;

                var header = new CrilaylaHeader() { UncompressedSize = inputSize };
                var compData = new MemoryStream();
                using (var bw = new BinaryWriterX(compData, true))
                {
                    int done_so_far = 0;
                    void WriteRaw() { bw.WriteBit(false); br.BaseStream.Position = br.BaseStream.Length - ++done_so_far; bw.WriteBits(br.ReadByte(), 8); };

                    void WriteBackref(byte[] backref, int backrefPos)
                    {
                        if (backref.Length < 3) throw new ArgumentException("Backref too short");

                        var end_backref = backrefPos + backref.Length;
                        var end_source = inputSize;

                        var offset = done_so_far - (end_source - end_backref) - 3;

                        long this_chunk = 0;
                        long leftover = backref.Length;
                        leftover -= 3;

                        bw.WriteBit(true);
                        bw.WriteBits(offset, 13);

                        int bits_max, bits = 2;
                        int[] next_bits = new int[] { 0, 0, 3, 5, 0, 8, 0, 0, 8 };
                        do
                        {
                            bits_max = (1 << bits) - 1;

                            this_chunk = (leftover > bits_max) ? bits_max : leftover;
                            leftover -= this_chunk;

                            bw.WriteBits(this_chunk, bits);

                            bits = next_bits[bits];
                        } while (this_chunk == bits_max);

                        done_so_far += backref.Length;
                    }

                    // Must do first 3 bytes raw
                    for (done_so_far = 0; done_so_far < 3 && done_so_far < inputSize;)
                        WriteRaw();

                    int sliding_window_size = 0x2000 + 2;
                    while (done_so_far < inputSize)
                    {
                        var needle_len = inputSize - done_so_far;
                        var backref_max = (done_so_far > sliding_window_size) ? sliding_window_size : done_so_far;

                        br.BaseStream.Position = 0x100;
                        var backrefInfo = LongestMatch(needle_len, br.ReadBytes(needle_len + backref_max));

                        if ((backrefInfo.backref?.Length ?? 0) < 3)
                            WriteRaw();
                        else
                        {
                            WriteBackref(backrefInfo.backref, backrefInfo.backrefPos);
                        }
                    }

                    bw.Flush();
                }

                // Compressed size excludes first 0x100 bytes
                header.CompressedSize = (int)compData.Length;

                var dest = new MemoryStream();
                using (var bw = new BinaryWriterX(dest, true))
                {
                    bw.WriteStruct(header);
                    compData.Position = 0;
                    var compDataRev = new BinaryReaderX(compData).ReadMultiple<int>((int)compData.Length / 4);
                    compDataRev.Reverse();
                    bw.WriteMultiple(compDataRev);
                    bw.Write(uncompressedData);
                }

                return dest.ToArray();
            }
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

            using (var br = new BinaryReaderX(input, true))
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
