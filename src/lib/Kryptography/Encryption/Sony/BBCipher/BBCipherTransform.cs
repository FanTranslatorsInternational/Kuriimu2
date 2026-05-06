using System.Security.Cryptography;

namespace Kryptography.Encryption.Sony.BBCipher
{
    public class BbCipherTransform : ICryptoTransform
    {
        private static readonly byte[] Loc1Ce4 = [0x13, 0x5F, 0xA4, 0x7C, 0xAB, 0x39, 0x5B, 0xA4, 0x76, 0xB8, 0xCC, 0xA9, 0x8F, 0x3A, 0x04, 0x45];
        private static readonly byte[] Loc1Cf4 = [0x67, 0x8D, 0x7F, 0xA3, 0x2A, 0x9C, 0xA0, 0xD1, 0x50, 0x8A, 0xD8, 0x38, 0x5E, 0x4B, 0x01, 0x7E];

        private readonly byte[] _kirkBuf = new byte[0x0814];

        private readonly bool _decrypt;
        private readonly int _type;

        private readonly byte[] _headerKey;
        private readonly byte[] _vkey;

        private readonly byte[] _key = new byte[0x10];

        public int Seed { get; set; }

        public BbCipherTransform(byte[] headerKey, byte[] vkey, int seed, int type, bool decrypt)
        {
            _headerKey = new byte[headerKey.Length];
            Array.Copy(headerKey, _headerKey, headerKey.Length);

            _vkey = new byte[vkey.Length];
            Array.Copy(vkey, _vkey, vkey.Length);

            _decrypt = decrypt;
            _type = type;
            Seed = seed;

            Initialize();
        }

        //Setup en-/decryption key out of 2 given keys
        private void Initialize()
        {
            if (_decrypt)
            {
                Seed++;
                Array.Copy(_headerKey, _key, 0x10);

                for (int i = 0; i < 0x10; i++)
                    _key[i] ^= _vkey[i];
            }
            else
            {
                Seed = 1;

                var res = Kirk.CryptKirk14(_kirkBuf);
                if (res != 0) throw new InvalidDataException(res.ToString("X8"));

                Array.Copy(_kirkBuf, 0, _kirkBuf, 0x14, 0x10);

                for (int i = 0; i < 0x10; i++)
                    _kirkBuf[0x14 + i] ^= Loc1Ce4[i];

                if (_type == 2)
                {
                    res = Kirk.EncryptWithFuse(_kirkBuf, 0x10);
                    if (res != 0) throw new InvalidDataException(res.ToString("X8"));
                }
                else
                {
                    res = Kirk.EncryptWith0(_kirkBuf, 0x10, 0x39);
                    if (res != 0) throw new InvalidDataException(res.ToString("X8"));
                }

                for (int i = 0; i < 0x10; i++)
                    _kirkBuf[0x14 + i] ^= Loc1Cf4[i];

                Array.Copy(_kirkBuf, 0x14, _key, 0, 0x10);
                //Array.Copy(_kirk_buf, 0x14, _header_key, 0, 0x10);

                for (int i = 0; i < 0x10; i++)
                    _key[i] ^= _vkey[i];
            }
        }

        public int InputBlockSize => 16;

        public int OutputBlockSize => 16;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (_decrypt)
                return DecryptBuffer(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            return EncryptBuffer(inputBuffer, inputCount, outputBuffer, outputOffset);
        }

        private int DecryptBuffer(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var offset = inputOffset;
            var outOffset = outputOffset;
            var size = inputCount;
            var seed = Seed;
            while (size > 0)
            {
                var dsize = Math.Min(0x800, size);

                var res = Sub428(inputBuffer, offset, dsize, outputBuffer, outOffset);
                if (res != 0) return inputCount - size;

                size -= dsize;
                offset += dsize;
                outOffset += dsize;
            }

            Seed = seed;
            return inputCount;
        }

        private int EncryptBuffer(byte[] inputBuffer, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var offset = 0;
            var size = inputCount;
            var seed = Seed;
            while (size > 0)
            {
                var dsize = Math.Min(0x800, size);

                var res = Sub428(inputBuffer, offset, dsize, outputBuffer, outputOffset);
                if (res != 0) return inputCount - size;

                size -= dsize;
                offset += dsize;
            }

            Seed = seed;
            return inputCount;
        }

        private uint Sub428(byte[] buffer, int offset, int size, byte[] outputBuf, int outputOffset)
        {
            byte[] tmp1 = new byte[0x10];
            byte[] tmp2 = new byte[0x10];

            Array.Copy(_key, 0, _kirkBuf, 0x14, 0x10);

            for (int i = 0; i < 0x10; i++)
                _kirkBuf[0x14 + i] ^= Loc1Cf4[i];

            uint res = _type == 2 ? Kirk.DecryptWithFuse(_kirkBuf, 0x10) : Kirk.DecryptWith0(_kirkBuf, 0x10, 0x39);
            if (res != 0) return res;

            for (int i = 0; i < 0x10; i++)
                _kirkBuf[i] ^= Loc1Ce4[i];

            Array.Copy(_kirkBuf, tmp2, 0x10);

            if (Seed == 1)
            {
                Array.Clear(tmp1, 0, 0x10);
            }
            else
            {
                Array.Copy(tmp2, tmp1, 0x10);
                Array.Copy(BitConverter.GetBytes(Seed - 1), 0, tmp1, 0xC, 0x4);
            }

            for (int i = 0; i < size; i += 0x10)
            {
                Array.Copy(tmp2, 0, _kirkBuf, 0x14 + i, 0xC);
                Array.Copy(BitConverter.GetBytes(Seed), 0, _kirkBuf, 0x14 + i + 0xC, 0x4);
                Seed += 1;
            }

            res = Sub1F8(size, tmp1, 0x63);
            if (res != 0) return res;

            for (int i = 0; i < size; i++)
                outputBuf[outputOffset + i] = (byte)(buffer[offset + i] ^ _kirkBuf[i]);

            return 0;
        }

        private uint Sub1F8(int size, byte[] key, int keyType)
        {
            byte[] tmp = new byte[0x10];

            Array.Copy(_kirkBuf, 0x14 + size - 0x10, tmp, 0, 0x10);

            var res = Kirk.DecryptWith0(_kirkBuf, size, keyType);
            if (res != 0) return res;

            for (int i = 0; i < 0x10; i++)
                _kirkBuf[i] ^= key[i];

            Array.Copy(tmp, key, 0x10);

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