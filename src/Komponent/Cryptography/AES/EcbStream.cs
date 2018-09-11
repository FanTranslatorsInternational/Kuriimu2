using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces;
using System.Security.Cryptography;

namespace Komponent.Cryptography.AES
{
    public class EcbStream : Stream, IKryptoStream
    {
        public int BlockSize => 128;

        public int BlockSizeBytes => 16;

        public List<byte[]> Keys { get; }

        public int KeySize => Keys[0]?.Length ?? 0;

        public byte[] IV => throw new NotSupportedException();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }

        private Stream _stream;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        public EcbStream(Stream input, byte[] key)
        {
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);

            var aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.ECB
            };
            _encryptor = aes.CreateEncryptor();
            _decryptor = aes.CreateDecryptor();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new InvalidDataException("Offset and count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");

            var blocksToRead = count / BlockSizeBytes + (count % BlockSizeBytes > 0 ? 1 : 0);
            var blockPaddedCount = blocksToRead * BlockSizeBytes;

            if (blockPaddedCount > _stream.Length - _stream.Position)
                throw new InvalidDataException($"Can't read {blockPaddedCount} bytes from position {Position} in stream with length {_stream.Length}.");

            var originalPosition = Position;
            Position = Position / BlockSizeBytes;
            var result = new byte[blockPaddedCount];
            for (int i = 0; i < blocksToRead; i++)
            {
                var tmp = new byte[BlockSizeBytes];
                _stream.Read(tmp, 0, BlockSizeBytes);
                var tmp2 = new byte[BlockSizeBytes];
                _decryptor.TransformBlock(tmp, 0, BlockSizeBytes, tmp2, 0);
            }

            var off = originalPosition - Position;
            buffer = result.Skip((int)off).Take(count).ToArray();
            Position = originalPosition + count;

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var enc = new byte[count];
            _encryptor.TransformBlock(buffer, offset, count, enc, 0);

            _stream.Write(enc, 0, enc.Length);

            //if (offset < 0 || count < 0)
            //    throw new InvalidDataException("Offset and count can't be negative.");
            //if (offset + count >= buffer.Length)
            //    throw new InvalidDataException("Buffer too short.");

            //var blocksToWrite = count / BlockSizeBytes + (count % BlockSizeBytes > 0 ? 1 : 0);
            //var blockPaddedCount = blocksToRead * BlockSizeBytes;
        }
    }
}
