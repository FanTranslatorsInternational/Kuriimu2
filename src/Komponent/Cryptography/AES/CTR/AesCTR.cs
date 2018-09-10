using System;
using System.Security.Cryptography;

namespace Komponent.Cryptography.AES
{
    public class CTR : SymmetricAlgorithm
    {
        private readonly byte[] _counter;
        private readonly AesManaged _aes;

        public CTR(byte[] ctr)
        {
            if (ctr == null)
                throw new ArgumentNullException("Counter cannot be null");
            if (ctr.Length != 16)
                throw new ArgumentException(string.Format("Counter must be the same size as a block (Expected {0}, got {1})", 16, ctr.Length));

            _counter = ctr;
            _aes = new AesManaged
            {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None,
                IV = ctr
            };
        }

        public override ICryptoTransform CreateEncryptor(byte[] key, byte[] notUsed = null)
        {
            return new CtrCryptoTransform(_aes, key, _counter);
        }

        public override ICryptoTransform CreateDecryptor(byte[] key, byte[] notUsed = null)
        {
            return new CtrCryptoTransform(_aes, key, _counter);
        }

        public override void GenerateKey()
        {
            _aes.GenerateKey();
        }

        public override void GenerateIV()
        {
            // Nope
        }

        public override int BlockSize { get => _aes.BlockSize; set => _aes.BlockSize = value; }

        public override byte[] Key { get => _aes.Key; set => _aes.Key = value; }
    }

    public class CtrCryptoTransform : ICryptoTransform
    {
        private byte[] _counter;
        private byte[] _origCounter;
        private readonly ICryptoTransform _encryptor;
        private readonly byte[] _buffer;
        private readonly SymmetricAlgorithm _symAlg;

        public CtrCryptoTransform(SymmetricAlgorithm sa, byte[] key, byte[] ctr)
        {
            if (sa == null)
                throw new ArgumentNullException("Symmetric Algorithm cannot be null");
            if (key == null)
                throw new ArgumentNullException("Key cannot be null");
            if (ctr == null)
                throw new ArgumentNullException("Counter cannot be null");
            if (ctr.Length != sa.BlockSize / 8)
                throw new ArgumentException(string.Format("Counter length must equal block size. (Expected {0}, got {1})", sa.BlockSize / 8, ctr.Length));

            _symAlg = sa;

            _counter = new byte[ctr.Length];
            ctr.CopyTo(_counter, 0);

            _origCounter = new byte[ctr.Length];
            ctr.CopyTo(_origCounter, 0);

            _encryptor = _symAlg.CreateEncryptor(key, new byte[0x10]);
            _buffer = new byte[0x400000]; // 4 MB Buffer
        }

        public void SeekCtr(long newOffset)
        {
            var ctrMod = newOffset / 0x10;

            _counter = new byte[_origCounter.Length];
            _origCounter.CopyTo(_counter, 0);

            for (int i = 0; i < ctrMod; i++)
                Increment();
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var output = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
            int outputOffset)
        {
            int blockLength;
            byte[] encryptedBuffer = new byte[_buffer.Length];
            for (int i = 0; i < inputCount; i += blockLength)
            {
                blockLength = inputCount - i > _buffer.Length ? _buffer.Length : inputCount - i;
                int passLen = blockLength % (_symAlg.BlockSize / 8) == 0
                    ? blockLength : blockLength + ((_symAlg.BlockSize / 8) - (blockLength % (_symAlg.BlockSize / 8)));
                _encryptor.TransformBlock(ManageBufferCounters(blockLength), 0, passLen, encryptedBuffer, 0);
                Array.Copy(encryptedBuffer, 0, outputBuffer, outputOffset + i, blockLength);
                for (int ofs = i; ofs < (i + blockLength) && ofs < inputCount; ofs++)
                {
                    outputBuffer[outputOffset + ofs] ^= inputBuffer[inputOffset + ofs];
                }
            }
            return inputCount;
        }

        public void Increment()
        {
            for (int i = _counter.Length - 1; i >= 0; i--)
            {
                if ((++_counter[i]) != 0)
                    return;
            }
        }

        public byte[] ManageBufferCounters(int size)
        {
            for (int i = 0; i < size; i += 0x10)
            {
                _counter.CopyTo(_buffer, i);
                Increment();
            }
            return _buffer;
        }

        public int InputBlockSize => _symAlg.BlockSize / 8;
        public int OutputBlockSize => _symAlg.BlockSize / 8;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => false;

        public void Dispose()
        {
        }
    }
}