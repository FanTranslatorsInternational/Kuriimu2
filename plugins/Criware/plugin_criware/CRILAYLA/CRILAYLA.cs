using System;
using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;

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
        private const int RawDataSize = 0x100;

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
                i = Array.IndexOf(haystack, starting_byte, i, haystack.Length - i);
                if (i < 0)
                    break;

                ConsiderPossibleMatch(i++);
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
            if (input.Length <= RawDataSize)
                throw new ArgumentException("Input needs to be longer than 256 bytes");

            using (var br = new BinaryReaderX(input))
            {
                var uncompressedData = br.ReadBytes(RawDataSize);

                var inputSize = (int)input.Length - RawDataSize;

                var header = new CrilaylaHeader() { UncompressedSize = inputSize };
                var maxCompLength = input.Length + inputSize / 8 + ((inputSize % 8 > 0) ? 1 : 0);

                var dest = new MemoryStream() { Position = 0x10 + maxCompLength };
                using (var bw = new BinaryWriterX(new ReverseStream(dest), true))
                {
                    int done_so_far = 0;

                    void WriteRaw()
                    {
                        bw.WriteBit(false);
                        br.BaseStream.Position = br.BaseStream.Length - ++done_so_far;
                        bw.WriteBits(br.ReadByte(), 8);
                    };
                    void WriteBackref(byte[] backref, int backrefPos)
                    {
                        if (backref.Length < 3) throw new ArgumentException("Backref too short");

                        var end_backref = backrefPos + backref.Length;

                        var offset = done_so_far - (inputSize - end_backref) - 3;

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

                            this_chunk = Math.Min(leftover, bits_max);
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
                        var backref_max = Math.Min(sliding_window_size, done_so_far);

                        br.BaseStream.Position = RawDataSize;
                        var backrefInfo = LongestMatch(needle_len, br.ReadBytes(needle_len + backref_max));

                        if ((backrefInfo.backref?.Length ?? 0) < 3)
                            WriteRaw();
                        else
                            WriteBackref(backrefInfo.backref, backrefInfo.backrefPos);
                    }

                    bw.Flush();
                }

                header.CompressedSize = (int)(dest.Length - dest.Position);

                var destStart = dest.Position -= 0x10;
                using (var bw = new BinaryWriterX(dest, true))
                {
                    bw.WriteType(header);
                    bw.BaseStream.Position += header.CompressedSize;
                    bw.Write(uncompressedData);
                }

                dest.Position = destStart;
                return new BinaryReaderX(dest).ReadBytes((int)(dest.Length - destStart));
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
                var header = br.ReadType<CrilaylaHeader>();

                if (header.Magic != "CRILAYLA" || header.Magic == "\0\0\0\0\0\0\0\0")
                    throw new InvalidOperationException();

                var lengthNeeded = RawDataSize + header.UncompressedSize;
                dest = new byte[lengthNeeded];

                br.BaseStream.Position = br.BaseStream.Length - RawDataSize;
                br.ReadBytes(RawDataSize).CopyTo(dest, 0);
            }

            using (var br = new BinaryReaderX(new ReverseStream(input), true))
            {
                // Actual decompression inits
                br.BaseStream.Position = br.BaseStream.Length - RawDataSize;
                int destPos = dest.Length;

                void BackwardCascadingCopy(int sourceIndex, int count)
                {
                    var d = destPos + count;
                    var s = sourceIndex + count;

                    while (count-- > 0)
                        dest[--d] = dest[--s];
                }

                // Actual decompression
                while (destPos > RawDataSize)
                {
                    if (br.ReadBit())
                    {
                        var initialOffset = br.ReadBits<short>(13);
                        var offset = destPos + initialOffset;
                        destPos -= 3;

                        int ReadOffset(int bits)
                        {
                            var value = br.ReadBits<int>(bits);
                            destPos -= value;
                            offset -= value;
                            return value;
                        };

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
