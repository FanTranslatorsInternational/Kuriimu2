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

        private long _length = 0;
        public override long Length => _length;
        private long TotalBlocks => CalculateBlockCount(Length);

        private long CalculateBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }

        private byte[] _lastBlockBuffer;
        private Stream _stream;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        public EcbStream(Stream input, byte[] key)
        {
            _lastBlockBuffer = new byte[BlockSizeBytes];
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);

            var aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros
            };
            _encryptor = aes.CreateEncryptor();
            _decryptor = aes.CreateDecryptor();
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
            //switch (origin)
            //{
            //    case SeekOrigin.Begin:
            //        _position = offset;
            //        break;
            //    case SeekOrigin.Current:
            //        _position += offset;
            //        break;
            //    case SeekOrigin.End:
            //        _position = Math.Max(_length - offset, 0);
            //        break;
            //}
            //_stream.Position = _position;

            //return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateInput(buffer, offset, count);

            var decrypted = ReadDecrypted(count);

            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;
            Array.Copy(decrypted.Skip((int)offsetIntoBlock).Take(count).ToArray(), 0, buffer, 0, count);
            Position += count;

            return count;
        }
        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new InvalidDataException("Offset and count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
            if (count > Length - Position)
                throw new InvalidDataException($"Can't read {count} bytes from position {Position} in stream with length {Length}.");
        }
        private byte[] ReadDecrypted(int count)
        {
            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToRead = CalculateBlockCount(offsetIntoBlock + count);
            var blockPaddedCount = blocksToRead * BlockSizeBytes;

            var originalPosition = Position;
            Position = Position / BlockSizeBytes;

            var tmp = new byte[blockPaddedCount];
            _stream.Read(tmp, 0, (int)blockPaddedCount);
            if (CalculateBlockCount(Position + count) >= TotalBlocks)
                Array.Copy(_lastBlockBuffer, 0, tmp, tmp.Length - BlockSizeBytes, _lastBlockBuffer.Length);

            var result = _decryptor.TransformFinalBlock(tmp, 0, (int)blockPaddedCount);

            Position = originalPosition;

            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToWrite = CalculateBlockCount(offsetIntoBlock + count);
            var blockPaddedCount = blocksToWrite * BlockSizeBytes;

            byte[] decrypted = new byte[blockPaddedCount];
            if (Position < Length)
                decrypted = ReadDecrypted(count);

            Array.Copy(buffer, 0, decrypted, offsetIntoBlock, count);
            var encrypted = _encryptor.TransformFinalBlock(decrypted, 0, decrypted.Length);

            if (CalculateBlockCount(Position + count) >= TotalBlocks)
                Array.Copy(encrypted.Skip(encrypted.Length - BlockSizeBytes).Take(BlockSizeBytes).ToArray(), _lastBlockBuffer, BlockSizeBytes);

            _length = Math.Max(_length, Position + count);
            _stream.Write(encrypted, (int)offsetIntoBlock, count);
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
