using System.IO;
using Kompression.IO;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class Dr3Decoder : IDecoder
    {
        private const int PreBufferSize_ = 0x3FA;

        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0x400)
            {
                Position = PreBufferSize_
            };

            var flagPosition = 0;
            var flag = 0;

            while (input.Position < input.Length)
            {
                if (flagPosition == 0)
                {
                    flag = input.ReadByte();
                    flagPosition = 8;
                }

                if ((flag >> --flagPosition & 1) == 1)
                {
                    // Read raw value
                    var value = (byte)input.ReadByte();

                    circularBuffer.WriteByte(value);
                    output.WriteByte(value);
                }
                else
                {
                    // Read match
                    var value = input.ReadByte() | (input.ReadByte() << 8);

                    var length = (value >> 10) + 2;
                    var displacement = 0x400 - (value & 0x3FF);

                    circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
