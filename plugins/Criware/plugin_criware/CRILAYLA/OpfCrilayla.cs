using Komponent.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_criware.CRILAYLA
{
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
            using (var br = new BinaryReaderX(src, ByteOrder.BigEndian, BitOrder.LSBFirst, 4))
            {
                var header = br.ReadStruct<OpfCrilaylaHeader>();

                if (header.Magic != "CRILAYLA" || header.Magic == "\0\0\0\0\0\0\0\0")
                    throw new InvalidOperationException();

                var lengthNeeded = 0x100 + header.UncompressedSize;
                var dest = new byte[lengthNeeded];

                br.BaseStream.Position = br.BaseStream.Length - 0x100;
                var uncompressedData = br.ReadBytes(0x100);
                uncompressedData.CopyTo(dest, 0);

                // Actual decompression inits
                br.BaseStream.Position = br.BaseStream.Length - 0x100 - br.BlockSize;
                var destPos = dest.Length;

                // Actual decompression
                while (destPos > 0x100)
                {
                    if (br.ReadBit())
                    {
                        var initialOffset = br.ReadBits<short>(13);
                    }
                    else
                    {
                        dest[--destPos] = br.ReadBits<byte>(8);
                    }
                }
            }

            /*
	BitReader br;
	br.init(source[0 .. $ - 256]);
	size_t dest_pos = dest.length;

	void backward_cascading_copy (size_t src_index, size_t num_bytes)
	{
		auto d = dest_pos + num_bytes;
		auto s = src_index + num_bytes;

		//writeln("\tstarting copy at dest ", d, ", source ", s);

		while (num_bytes--)
			dest[--d] = dest[--s];
	}

	while (dest_pos > 256)
	{
		auto is_backref = br.readBit();
		if (is_backref)
		{
			uint initial_offset = br.read(13);
			size_t offset = dest_pos + initial_offset;
			dest_pos -= 3;

			uint read_offset(uint bits)
			{
				uint value = br.read(bits);
				dest_pos -= value;
				offset -= value;
				return value;
			}

			uint more;
			if (initial_offset >= 3)
			{
				// No overlap between the two possible, combine.
				more = read_offset(2);
				memmove(dest.ptr + dest_pos, dest.ptr + offset, 3 + more);
			}
			else
			{
				// Two copies, for overlap (but internal overlap of these two is not possible.)
				memmove(dest.ptr + dest_pos, dest.ptr + offset, 3);
				more = read_offset(2);
				memmove(dest.ptr + dest_pos, dest.ptr + offset, more);
			}

			if (more == 3)
			{
				more = read_offset(3);
				// Note any value > 3 could cause a cascade for the below.
				backward_cascading_copy(offset, more);

				if (more == 7)
				{
					more = read_offset(5);
					backward_cascading_copy(offset, more);

					if (more == 31)
					{
						do
						{
							more = read_offset(8);
							backward_cascading_copy(offset, more);
						}
						while (more == 255);
					}
				}
			}
		}
		else
		{
			dest[--dest_pos] = cast(ubyte)br.readByte();
		}
	}

	return dest;*/
        }
    }
}
