using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Kontract.Abstracts;

namespace Komponent.Cryptography.AES
{
    public class EcbStream : KryptoStream
    {
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;
        private byte[] _finalBlock;
        private long _length = 0;
        private Stream _stream;
        public EcbStream(Stream input, byte[] key)
        {
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);

            var aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };
            _decryptor = aes.CreateDecryptor();
            _encryptor = aes.CreateEncryptor();

            _finalBlock = new byte[BlockSizeBytes];
        }

        public override int BlockSize => 128;

        public override int BlockSizeBytes => BlockSize / 8;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override byte[] IV => throw new NotImplementedException();
        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override long Length => _length;

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }
        private int TotalBlocks => CalculateBlockCount((int)Length);
        public override void Flush()
        {
            if (Position % BlockSizeBytes > 0)
                Position -= Position % BlockSizeBytes;
            _stream.Write(_finalBlock, 0, _finalBlock.Length);
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToWrite = CalculateBlockCount((int)offsetIntoBlock + count);
            var blockPaddedCount = blocksToWrite * BlockSizeBytes;

            byte[] decrypted = ReadDecrypted(count);
            Array.Copy(buffer, 0, decrypted, offsetIntoBlock, count);

            if (CalculateBlockCount((int)Length) < CalculateBlockCount((int)Position) - 1)
            {
                var betweenBlocks = CalculateBlockCount((int)Position) - CalculateBlockCount((int)Length) - 1;
                var newDecrypted = new byte[betweenBlocks * BlockSizeBytes + decrypted.Length];
                Array.Copy(decrypted, 0, newDecrypted, betweenBlocks * BlockSizeBytes, decrypted.Length);
                decrypted = newDecrypted;
            }

            var encrypted = _encryptor.TransformFinalBlock(decrypted, 0, decrypted.Length);

            if (CalculateBlockCount((int)Position + count) >= TotalBlocks)
                Array.Copy(encrypted.Skip(encrypted.Length - BlockSizeBytes).Take(BlockSizeBytes).ToArray(), _finalBlock, BlockSizeBytes);

            var originalPosition = Position;
            if (CalculateBlockCount((int)Length) < CalculateBlockCount((int)Position) - 1)
                Position -= (CalculateBlockCount((int)Position) - CalculateBlockCount((int)Length) - 1) * BlockSizeBytes;
            Position = Position - offsetIntoBlock;

            _stream.Write(encrypted, 0, encrypted.Length);

            _length = Math.Max(_length, Position + count);
            Position = originalPosition + count;
        }

        private int CalculateBlockCount(int input) => (int)Math.Ceiling((double)input / BlockSizeBytes);
        private byte[] ReadDecrypted(int count)
        {
            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToRead = CalculateBlockCount((int)offsetIntoBlock + count);
            var blockPaddedCount = blocksToRead * BlockSizeBytes;

            if (Length <= 0 || count <= 0)
                return new byte[blockPaddedCount];

            var originalPosition = Position;
            Position -= offsetIntoBlock;

            var minimalDecryptableSize = Math.Min(CalculateBlockCount((int)Length) * BlockSizeBytes, blockPaddedCount);
            var bytesRead = new byte[minimalDecryptableSize];
            _stream.Read(bytesRead, 0, minimalDecryptableSize);
            Position = originalPosition;

            if (CalculateBlockCount((int)Position + count) >= TotalBlocks)
                Array.Copy(_finalBlock, 0, bytesRead, bytesRead.Length - BlockSizeBytes, _finalBlock.Length);

            var decrypted = _decryptor.TransformFinalBlock(bytesRead, 0, minimalDecryptableSize);
            var result = new byte[blockPaddedCount];
            Array.Copy(decrypted, 0, result, 0, minimalDecryptableSize);

            return result;
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
    }
}
