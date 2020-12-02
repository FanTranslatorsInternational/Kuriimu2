using System.IO;

namespace Kryptography
{
    public class PositionalXorStream : XorStream
    {
        public PositionalXorStream(Stream input, byte[] key) : base(input, key)
        {
        }

        protected override void FillXorBuffer(byte[] fill, long pos, byte[] key)
        {
            base.FillXorBuffer(fill, pos, key);

            for (var i = pos; i < fill.Length + pos; i++)
                fill[i] ^= (byte)i;
        }
    }
}
