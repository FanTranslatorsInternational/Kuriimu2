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
        long Position { get; set; }

        int BlockSize { get; }

        byte[] Key { get; }
        int KeySize { get; }

        byte[] IV { get; }

        long Seek(long offset, SeekOrigin origin);
        int Read(byte[] buffer, int offset, int count);
        int ReadByte();
        void Write(byte[] buffer, int offset, int count);
        void WriteByte(byte value);
    }
}
