using System.IO;
using System.Linq;

namespace Kompression.Implementations.Decoders.Headerless
{
    public class RleHeaderlessDecoder
    {
        public void Decode(Stream input, Stream output, int decompressedSize)
        {
            while (output.Length < decompressedSize)
            {
                var flag = input.ReadByte();
                if ((flag & 0x80) > 0)
                {
                    var repetitions = (flag & 0x7F) + 3;
                    output.Write(Enumerable.Repeat((byte)input.ReadByte(), repetitions).ToArray(), 0, repetitions);
                }
                else
                {
                    var length = flag + 1;
                    var uncompressedData = new byte[length];
                    input.Read(uncompressedData, 0, length);
                    output.Write(uncompressedData, 0, length);
                }
            }
        }
    }
}
