using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

/* Used in [PS2 Game] and MTV archive */
// TODO: Find out that PS2 game from IcySon55

namespace Kompression.LempelZiv
{
    public static class LZSSVLC
    {
        public static void Decompress(Stream input, Stream output)
        {
            var decompressedSize = ReadVlc(input);
            var unk1 = ReadVlc(input); // filetype maybe???
            var unk2 = ReadVlc(input); // compression type = 1 (LZSS?)

            while (output.Position < decompressedSize)
            {
                var value = input.ReadByte();
                var compressedBlocks = ReadVlc(input, value >> 4);
                var size = ReadVlc(input, value & 0xF);

                for (var i = 0; i < size; i++)
                    output.WriteByte((byte)input.ReadByte());

                for (var i = 0; i < compressedBlocks; i++)
                {
                    value = input.ReadByte();
                    var length = ReadVlc(input, value >> 4);
                    var offset = ReadVlc(input, value & 0xF);

                    var currentPosition = output.Position;
                    var copyPosition = currentPosition - offset;
                    for (var j = 0; j < length; j++)
                    {
                        output.Position = copyPosition + j;
                        var copyValue = output.ReadByte();
                        output.Position = currentPosition + j;
                        output.WriteByte((byte)copyValue);
                    }
                }
            }
        }

        private static int ReadVlc(Stream input, int initialValue = 0)
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
    }
}
