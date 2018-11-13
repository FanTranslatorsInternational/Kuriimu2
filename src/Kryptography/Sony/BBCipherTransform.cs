using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Kryptography.Sony
{
    public class BBCipherTransform : ICryptoTransform
    {
        bool _decrypt;
        byte[] _key;

        public BBCipherTransform(byte[] key, bool decrypt)
        {
            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);

            _decrypt = decrypt;
        }

        public int InputBlockSize => 16;

        public int OutputBlockSize => 16;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        public void Dispose()
        {
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (_decrypt)
                return DecryptBuffer(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            else
                return EncryptBuffer(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        //TODO: Implement Decryption
        private int DecryptBuffer(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return 0;
        }

        //TODO: Implement Encryption
        private int EncryptBuffer(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return 0;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            return outputBuffer;
        }
    }
}
