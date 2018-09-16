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
    public class CtrStream : KryptoStream
    {
        private byte[] _counter;
        private ICryptoTransform _encryptor;
        private byte[] _finalBlock;
        private long _length = 0;
        private long _offset = 0;
        private Stream _stream;

        public CtrStream(byte[] input, long offset, long length, byte[] key, byte[] counter) : this(new MemoryStream(input), offset, length, key, counter) { }

        public CtrStream(Stream input, long offset, long length, byte[] key, byte[] counter)
        {
            _stream = input;
            _length = length;
            _offset = offset;

            Keys = new List<byte[]>();
            Keys.Add(key);

            _counter = IV = counter;

            var aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };
            _encryptor = aes.CreateEncryptor();

            _finalBlock = new byte[BlockSizeBytes];

            Position = input.Position;
        }

        public CtrStream(byte[] input, byte[] key, byte[] counter) : this(new MemoryStream(input), key, counter) { }

        public CtrStream(Stream input, byte[] key, byte[] counter)
        {
            _stream = input;
            _length = _stream.Length;

            Keys = new List<byte[]>();
            Keys.Add(key);

            _counter = IV = counter;

            var aes = new AesManaged
            {
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };
            _encryptor = aes.CreateEncryptor();

            _finalBlock = new byte[BlockSizeBytes];

            Position = input.Position;
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

        public override long Position
        {
            get => _stream.Position - _offset;
            set => Seek(value, SeekOrigin.Begin);
        }
        private int TotalBlocks => CalculateBlockCount((int)Length);
        public override void Flush()
        {
            if (Position % BlockSizeBytes > 0)
                Position -= Position % BlockSizeBytes;
            _stream.Write(_finalBlock, 0, _finalBlock.Length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SeekCtr(Position);

            ValidateInput(buffer, offset, count);

            var decrypted = ReadDecrypted(count);

            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;
            Array.Copy(decrypted.Skip((int)offsetIntoBlock).Take(count).ToArray(), 0, buffer, offset, count);
            Position += count;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    SeekCtr(offset);
                    break;
                case SeekOrigin.Current:
                    SeekCtr(Position + offset);
                    break;
                case SeekOrigin.End:
                    SeekCtr(Length + offset);
                    break;
            }

            return _stream.Seek(offset + _offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            SeekCtr(Position);

            long offsetIntoBlock = 0;
            if (Position % BlockSizeBytes > 0)
                offsetIntoBlock = Position % BlockSizeBytes;

            var blocksToWrite = CalculateBlockCount((int)offsetIntoBlock + count);
            var blockPaddedCount = blocksToWrite * BlockSizeBytes;

            byte[] decrypted = ReadDecrypted(count);
            Array.Copy(buffer, offset, decrypted, offsetIntoBlock, count);

            if (CalculateBlockCount((int)Length) < CalculateBlockCount((int)Position) - 1)
            {
                var betweenBlocks = CalculateBlockCount((int)Position) - CalculateBlockCount((int)Length) - 1;
                var newDecrypted = new byte[betweenBlocks * BlockSizeBytes + decrypted.Length];
                Array.Copy(decrypted, 0, newDecrypted, betweenBlocks * BlockSizeBytes, decrypted.Length);
                decrypted = newDecrypted;
            }

            var encrypted = Crypt(decrypted);

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

        private int CalculateCurrentBlock(int input) => (int)Math.Floor((double)input / BlockSizeBytes);

        private byte[] Crypt(byte[] plain)
        {
            var originalPosition = Position;
            if (CalculateBlockCount((int)Length) < CalculateBlockCount((int)Position) - 1)
                Position -= (CalculateBlockCount((int)Position) - CalculateBlockCount((int)Length) - 1) * BlockSizeBytes;
            Position = Position - Position % BlockSizeBytes;

            byte[] result = new byte[plain.Length];
            plain.CopyTo(result, 0);

            for (int i = 0; i < plain.Length; i += BlockSizeBytes)
            {
                var ctrKey = _encryptor.TransformFinalBlock(_counter, 0, _counter.Length);
                IncrementCtr();

                for (int j = 0; j < BlockSizeBytes; j++)
                    result[i + j] ^= ctrKey[j];
            }

            Position = originalPosition;

            return result;
        }

        private void IncrementCtr(int count = 1)
        {
            for (int j = 0; j < count; j++)
                for (int i = _counter.Length - 1; i >= 0; i--)
                {
                    if ((++_counter[i]) != 0)
                        break;
                }
        }

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

            var decrypted = Crypt(bytesRead);

            var result = new byte[blockPaddedCount];
            Array.Copy(decrypted, 0, result, 0, minimalDecryptableSize);

            return result;
        }

        private void SeekCtr(long newOffset)
        {
            var ctrMod = CalculateCurrentBlock((int)newOffset);

            _counter = new byte[IV.Length];
            IV.CopyTo(_counter, 0);

            IncrementCtr(ctrMod);
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
