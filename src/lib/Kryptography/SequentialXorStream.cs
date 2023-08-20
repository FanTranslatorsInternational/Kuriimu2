using System.IO;

namespace Kryptography
{
    public class SequentialXorStream : XorStream
    {
        public SequentialXorStream(Stream input, byte key, byte step) : base(input, GetStepBuffer(key, step))
        {
        }

        private static byte[] GetStepBuffer(byte key, byte step)
        {
            var size = GetSize(key, step);
            var buffer = new byte[size];

            var current = key;
            for (var i = 0; i < size; i++)
            {
                buffer[i] = current;
                current += step;
            }

            return buffer;
        }

        private static int GetSize(byte key, byte step)
        {
            var current = key;

            var size = 1;
            while ((current = (byte)(current + step)) != key)
                size++;

            return size;
        }
    }
}
