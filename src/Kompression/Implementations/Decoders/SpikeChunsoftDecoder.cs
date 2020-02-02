using System.IO;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class SpikeChunsoftDecoder : IDecoder
    {
        private CircularBuffer _circularBuffer;
        private int _displacement;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var magic = (uint)buffer.GetInt32BigEndian(0);
            if (magic != 0xFCAA55A7)
                throw new InvalidCompressionException("Spike Chunsoft");

            input.Read(buffer, 0, 4);
            var decompressedSize = buffer.GetInt32LittleEndian(0);
            input.Read(buffer, 0, 4);
            var compressedSize = buffer.GetInt32LittleEndian(0);

            _circularBuffer = new CircularBuffer(0x1FFF);

            while (output.Position < decompressedSize)
            {
                var flag = (byte)input.ReadByte();

                if ((flag & 0x80) == 0x80)
                {
                    // Lz match start
                    ReadMatchStart(input, output, flag);
                }
                else if ((flag & 0x60) == 0x60)
                {
                    // Lz match continue
                    ReadMatchContinue(output, flag);
                }
                else if ((flag & 0x40) == 0x40)
                {
                    // Rle data
                    ReadRle(input, output, flag);
                }
                else
                {
                    // Raw data
                    ReadRawData(input, output, flag);
                }
            }
        }

        private void ReadMatchStart(Stream input, Stream output, byte flag)
        {
            // Min length: 4, Max length: 7
            // Min disp: 0, Max disp: 0x1FFF

            var length = ((flag >> 5) & 0x3) + 4;
            _displacement = (flag & 0x1F) << 8;
            _displacement |= input.ReadByte();

            _circularBuffer.Copy(output, _displacement, length);
        }

        private void ReadMatchContinue(Stream output, byte flag)
        {
            // Min length: 0, Max length: 0x1F

            var length = flag & 0x1F;

            _circularBuffer.Copy(output, _displacement, length);
        }

        private void ReadRle(Stream input, Stream output, byte flag)
        {
            // Min length: 4, Max length: 0x1003

            int length;
            if ((flag & 0x10) == 0x00)
                length = (flag & 0xF) + 4;
            else
                length = ((flag & 0xF) << 8) + input.ReadByte() + 4;

            var value = (byte)input.ReadByte();

            for (var i = 0; i < length; i++)
            {
                output.WriteByte(value);
                _circularBuffer.WriteByte(value);
            }
        }

        private void ReadRawData(Stream input, Stream output, byte flag)
        {
            // Min length: 0, Max length: 0x1FFF

            int length;
            if ((flag & 0x20) == 0x00)
                length = flag & 0x1F;
            else
                length = ((flag & 0x1F) << 8) + input.ReadByte();

            for (var i = 0; i < length; i++)
            {
                var nextValue = (byte)input.ReadByte();

                output.WriteByte(nextValue);
                _circularBuffer.WriteByte(nextValue);
            }
        }

        public void Dispose()
        {
            _circularBuffer?.Dispose();
            _circularBuffer = null;
        }
    }
}
