using System.IO;

namespace Kryptography
{
    public class PositionalXorStream : XorStream
    {
        public PositionalXorStream(Stream input, byte[] key) : base(input, key)
        { }

        protected override void FillXorBuffer(byte[] fill, long pos, byte[] key)
        {
            base.FillXorBuffer(fill, pos, key);

            for (var i = 0; i < fill.Length; i++)
                fill[i] ^= (byte)(pos + i);
        }
    }
}
