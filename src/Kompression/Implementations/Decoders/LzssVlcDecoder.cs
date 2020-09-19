using System.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class LzssVlcDecoder : IDecoder
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

                var rawBuffer = new byte[size];
                input.Read(rawBuffer, 0, size);
                output.Write(rawBuffer, 0, size);

                for (var i = 0; i < compressedBlocks; i++)
                {
                    value = input.ReadByte();
                    var offset = ReadVlc(input, value & 0xF);   // yes, this one is the only one seemingly using this scheme of reading a value
                    offset++;
                    var length = value >> 4 > 0 ? value >> 4 : ReadVlc(input);
                    length++;

                    CopyBytes(output, (int)output.Position - offset, (int)output.Position, length);
                }
            }
        }

        private int ReadVlc(Stream input, int initialValue = 0)
        {
            var result = initialValue >> 1;
            while (initialValue % 2 == 0)
                result = (result << 7) | ((initialValue = input.ReadByte()) >> 1);
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
