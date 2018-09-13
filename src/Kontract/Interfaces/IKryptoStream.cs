using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces
{
    public interface IKryptoStream
    {
        int BlockSize { get; }
        int BlockSizeBytes { get; }

        List<byte[]> Keys { get; }
        int KeySize { get; }

        byte[] IV { get; }

        byte[] ReadBytes(int count);
        void WriteBytes(byte[] input);
    }
}
