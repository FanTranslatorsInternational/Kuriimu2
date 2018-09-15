using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces;
using System.Security.Cryptography;
using Kontract.Abstracts;

namespace Komponent.Cryptography.AES
{
    public class CbcStream : KryptoStream
    {
        private AesManaged _aes;
        private byte[] _finalBlock;
        private long _length = 0;
        private Stream _stream;
        public CbcStream(Stream input, byte[] key, byte[] iv)
        {
            _finalBlock = new byte[BlockSizeBytes];
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = iv;

            _aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.CBC,
                IV = iv,
                Padding = PaddingMode.Zeros
            };
        }

        public override int BlockSize => 128;

        public override int BlockSizeBytes => BlockSize / 8;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override byte[] IV { get; }
        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override long Length => _length;
        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }
        private long TotalBlocks => CalculateBlockCount(Length);

        public override void Close()
        {
            Dispose();
        }

        public new void Dispose()
        {
            Flush();

            _stream.Dispose();
            _finalBlock = null;
        }

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
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position < Length)
                throw new NotSupportedException("Editing already existing data is not supported.");

            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var positionToBegin = Position - offsetIntoBlock;
            var blocksToWrite = CalculateBlockCount(offsetIntoBlock + count);
            var blockPaddedCount = blocksToWrite * BlockSizeBytes;

            byte[] decrypted = ReadDecrypted(count);
            Array.Copy(buffer, 0, decrypted, offsetIntoBlock, count);

            if (CalculateBlockCount(Length) < CalculateBlockCount(Position) - 1)
            {
                var betweenBlocks = CalculateBlockCount(Position) - CalculateBlockCount(Length) - 1;
                positionToBegin -= betweenBlocks * BlockSizeBytes;
                var newDecrypted = new byte[betweenBlocks * BlockSizeBytes + decrypted.Length];
                Array.Copy(decrypted, 0, newDecrypted, betweenBlocks * BlockSizeBytes, decrypted.Length);
                decrypted = newDecrypted;
            }

            var encrypted = CreateEncryptor(GetStartIV((int)positionToBegin)).TransformFinalBlock(decrypted, 0, decrypted.Length);

            if (CalculateBlockCount(Position + count) >= TotalBlocks)
                Array.Copy(encrypted.Skip(encrypted.Length - BlockSizeBytes).Take(BlockSizeBytes).ToArray(), _finalBlock, BlockSizeBytes);

            var originalPosition = Position;
            if (CalculateBlockCount(Length) < CalculateBlockCount(Position) - 1)
                Position -= (CalculateBlockCount(Position) - CalculateBlockCount(Length) - 1) * BlockSizeBytes;
            Position = Position - offsetIntoBlock;

            _stream.Write(encrypted, 0, encrypted.Length);

            _length = Math.Max(_length, originalPosition + count);
            Position = originalPosition + count;
        }

        private long CalculateBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);
        private ICryptoTransform CreateDecryptor(byte[] iv)
        {
            _aes.IV = iv;
            return _aes.CreateDecryptor();
        }

        private ICryptoTransform CreateEncryptor(byte[] iv)
        {
            _aes.IV = iv;
            return _aes.CreateEncryptor();
        }

        private byte[] GetStartIV(int blockToBeginWith)
        {
            if (blockToBeginWith <= 1)
                return IV;
            else
            {
                var iv = new byte[BlockSizeBytes];

                var originalPosition = Position;
                Position = (blockToBeginWith - 1) * BlockSizeBytes;
                _stream.Read(iv, 0, iv.Length);

                Position = originalPosition;

                return iv;
            }
        }
        private byte[] ReadDecrypted(int count)
        {
            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToRead = CalculateBlockCount(offsetIntoBlock + count);
            var blockPaddedCount = blocksToRead * BlockSizeBytes;

            if (Length <= 0 || count <= 0)
                return new byte[blockPaddedCount];

            var originalPosition = Position;
            Position -= offsetIntoBlock;

            var minimalDecryptableSize = (int)Math.Min(CalculateBlockCount(Length) * BlockSizeBytes, (int)blockPaddedCount);
            var bytesRead = new byte[minimalDecryptableSize];
            _stream.Read(bytesRead, 0, minimalDecryptableSize);
            Position = originalPosition;

            if (CalculateBlockCount(Position + count) >= TotalBlocks)
                Array.Copy(_finalBlock, 0, bytesRead, bytesRead.Length - BlockSizeBytes, _finalBlock.Length);

            var decrypted = CreateDecryptor(GetStartIV((int)CalculateBlockCount(Position))).TransformFinalBlock(bytesRead, 0, minimalDecryptableSize);
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
