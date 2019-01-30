using System;
using System.IO;
using Komponent.IO;

namespace plugin_criware.CRILAYLA
{
    /// <summary>
    /// The basic CRILAYLA header.
    /// </summary>
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
        private const int HeaderLength = 16;
        private const int UncompressedDataLength = 256;

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
            using (var brx = new BinaryReaderX(input, true))
            {
                // Read in the CRILAYLA header.
                var header = brx.ReadStruct<CrilaylaHeader>();

                // Make sure the data isn't bogus.
                if (header.Magic != "CRILAYLA" && header.Magic != "\0\0\0\0\0\0\0\0")
                    throw new FormatException("The stream provided does not appear to be a CRILAYLA compressed stream.");

                // Uncompressed data
                var dest = new byte[header.UncompressedSize];

                // Read uncompressed file portion
                brx.BaseStream.Position = brx.BaseStream.Length - UncompressedDataLength;
                var ufp = brx.ReadBytes(UncompressedDataLength);
                Array.Copy(ufp, 0, dest, 0, UncompressedDataLength);

                // Initialize our 
                brx.BaseStream.Position = HeaderLength;
                var source = brx.ReadBytes(header.CompressedSize);
                Array.Copy(source, 0, dest, UncompressedDataLength, header.CompressedSize);

                // BitReader
                var br = new BitReader(source);
                var destPos = (uint)dest.Length;

                void BackwardsCascadingCopy(uint sourcePos, uint length)
                {
                    var d = destPos + length;
                    var s = sourcePos + length;

                    while (length-- > 0)
                        dest[--d] = dest[--s];
                }

                while (destPos > 256)
                {
                    var isBackref = br.ReadBit();
                    if (isBackref)
                    {
                        var initialOffset = br.Read(13);
                        var offset = destPos + initialOffset;
                        destPos -= 3;

                        uint ReadOffset(uint bits)
                        {
                            uint value = 0; br.Read(bits);
                            destPos -= value;
                            offset -= value;
                            return value;
                        }

                        uint more;
                        if (initialOffset >= 3)
                        {
                            // No overlap between the two possible, combine.
                            more = ReadOffset(2);
                            //memmove(dest.ptr + dest_pos, dest.ptr + offset, 3 + more);
                        }
                        else
                        {
                            // Two copies, for overlap (but internal overlap of these two is not possible.)
                            //memmove(dest.ptr + dest_pos, dest.ptr + offset, 3);
                            more = ReadOffset(2);
                            //memmove(dest.ptr + dest_pos, dest.ptr + offset, more);
                        }

                        if (more == 3)
                        {
                            more = ReadOffset(3);
                            // Note any value > 3 could cause a cascade for the below.
                            BackwardsCascadingCopy(offset, more);

                            if (more == 7)
                            {
                                more = ReadOffset(5);
                                BackwardsCascadingCopy(offset, more);

                                if (more == 31)
                                {
                                    do
                                    {
                                        more = ReadOffset(8);
                                        BackwardsCascadingCopy(offset, more);
                                    } while (more == 255);

                                }
                            }
                        }
                    }
                    else
                    {
                        dest[--destPos] = (byte)br.ReadByte();
                    }
                }

                return dest;
            }
        }
    }
}
