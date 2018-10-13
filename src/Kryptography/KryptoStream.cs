using System.Collections.Generic;
using System.IO;

namespace Kryptography
{
    public abstract class KryptoStream : Stream
    {
        public abstract int BlockSize { get; }
        public abstract int BlockSizeBytes { get; }

        public abstract List<byte[]> Keys { get; }
        public abstract int KeySize { get; }

        public abstract byte[] IV { get; }
    }
}