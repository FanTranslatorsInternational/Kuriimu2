using System.IO;

namespace Kompression.LempelZiv.Decoders
{
    class LzssVlcDecoder : ILzDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var decompressedSize = ReadVlc(input);
            ReadVlc(input); // filetype maybe???
            ReadVlc(input); // compression type = 1 (LZSS?)

            while (output.Position < decompressedSize)
            {
                var value = input.ReadByte();
                var size = (value & 0xF) > 0 ? value & 0xF : ReadVlc(input);
                var compressedBlocks = value >> 4 > 0 ? value >> 4 : ReadVlc(input);

                for (var i = 0; i < size; i++)
                    output.WriteByte((byte)input.ReadByte());

                for (var i = 0; i < compressedBlocks; i++)
                {
                    value = input.ReadByte();
                    var offset = ReadVlc(input, value & 0xF);   // yes, this one is the only one seemingly using this scheme of reading a value
                    var length = value >> 4 > 0 ? value >> 4 : ReadVlc(input);
                    length += 1;

                    CopyBytes(output, (int)output.Position - offset, (int)output.Position, length);
                }
            }
        }

        private int ReadVlc(Stream input, int initialValue = 0)
        {
            var flag = initialValue & 0x1;
            var result = initialValue >> 1;
            if (flag == 1)
                return result;

            byte next;
            do
            {
                next = (byte)input.ReadByte();
                result <<= 7;
                result |= next >> 1;
            } while ((next & 0x1) == 0);

            return result;
        }

        private void CopyBytes(Stream output, int from, int to, int length)
        {
            for (int i = from, j = to; i < from + length; i++, j++)
            {
                output.Position = i;
                var copyValue = output.ReadByte();
                output.Position = j;
                output.WriteByte((byte)copyValue);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
