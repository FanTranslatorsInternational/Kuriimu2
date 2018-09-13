using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using Komponent.Cryptography.AES;
using Komponent.Cryptography.AES.XTS;
using Kontract.Interfaces;

namespace Komponent.Cryptography
{
    public class CtrStream : Stream, IKryptoStream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }

        public override long Length => _stream.Length;

        public int BlockSize => 128;

        public int BlockSizeBytes => 16;

        public int KeySize => Keys[0]?.Length ?? 0;

        public byte[] IV { get; }

        public List<byte[]> Keys { get; }

        CryptoStream _decryptor = null;
        CryptoStream _encryptor = null;

        CtrCryptoTransform _ctrDecryptor = null;
        CtrCryptoTransform _ctrEncryptor = null;

        Stream _stream;

        public CtrStream(Stream input, byte[] key, byte[] ctr)
        {
            _stream = input;
            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = ctr;

            var ctrContext = new CTR(ctr);

            _ctrDecryptor = ctrContext.CreateDecryptor(key) as CtrCryptoTransform;
            _ctrEncryptor = ctrContext.CreateEncryptor(key) as CtrCryptoTransform;

            _decryptor = new CryptoStream(_stream, _ctrDecryptor, CryptoStreamMode.Read);
            _encryptor = new CryptoStream(_stream, _ctrEncryptor, CryptoStreamMode.Write);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _decryptor.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _ctrDecryptor.SeekCtr(offset);
                    _ctrEncryptor.SeekCtr(offset);
                    break;
                case SeekOrigin.Current:
                    _ctrDecryptor.SeekCtr(_stream.Position + offset);
                    _ctrEncryptor.SeekCtr(_stream.Position + offset);
                    break;
                case SeekOrigin.End:
                    _ctrDecryptor.SeekCtr(_stream.Length + offset);
                    _ctrEncryptor.SeekCtr(_stream.Length + offset);
                    break;
            }

            return _stream.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _encryptor.Write(buffer, offset, count);
            _encryptor.Flush();
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            Read(buffer, 0, count);
            return buffer;
        }

        public void WriteBytes(byte[] input)
        {
            Write(input, 0, input.Length);
        }
    }
}
