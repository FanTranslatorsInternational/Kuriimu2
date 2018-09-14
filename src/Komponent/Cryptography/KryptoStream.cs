using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Komponent.Cryptography
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
