using System;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.Sony.BBCipher
{
    public class BBCipherTransform : ICryptoTransform
    {
        private static byte[] loc_1CE4 = { 0x13, 0x5F, 0xA4, 0x7C, 0xAB, 0x39, 0x5B, 0xA4, 0x76, 0xB8, 0xCC, 0xA9, 0x8F, 0x3A, 0x04, 0x45 };
        private static byte[] loc_1CF4 = { 0x67, 0x8D, 0x7F, 0xA3, 0x2A, 0x9C, 0xA0, 0xD1, 0x50, 0x8A, 0xD8, 0x38, 0x5E, 0x4B, 0x01, 0x7E };

        private byte[] _kirk_buf = new byte[0x0814];

        private bool _decrypt;
        private int _type;

        private byte[] _header_key;
        private byte[] _vkey;

        private byte[] _key = new byte[0x10];

        private int _seed;
        public int Seed { get => _seed; set => _seed = value; }

        public BBCipherTransform(byte[] header_key, byte[] vkey, int seed, int type, bool decrypt)
        {
            _header_key = new byte[header_key.Length];
            Array.Copy(header_key, _header_key, header_key.Length);

            _vkey = new byte[vkey.Length];
            Array.Copy(vkey, _vkey, vkey.Length);

            _decrypt = decrypt;
            _type = type;
            _seed = seed;

            Initialize();
        }

        //Setup en-/decryption key out of 2 given keys
        private void Initialize()
        {
            if (_decrypt == true)
            {
                _seed++;
                Array.Copy(_header_key, _key, 0x10);
                if (_vkey != null)
                    for (int i = 0; i < 0x10; i++)
                        _key[i] ^= _vkey[i];
            }
            else
            {
                _seed = 1;

                var res = Kirk.CryptKirk14(_kirk_buf);
                if (res != 0) throw new InvalidDataException(res.ToString("X8"));

                Array.Copy(_kirk_buf, 0, _kirk_buf, 0x14, 0x10);

                for (int i = 0; i < 0x10; i++)
                    _kirk_buf[0x14 + i] ^= loc_1CE4[i];

                if (_type == 2)
                {
                    res = Kirk.EncryptWithFuse(_kirk_buf, 0x10);
                    if (res != 0) throw new InvalidDataException(res.ToString("X8"));
                }
                else
                {
                    res = Kirk.EncryptWith0(_kirk_buf, 0x10, 0x39);
                    if (res != 0) throw new InvalidDataException(res.ToString("X8"));
                }

                for (int i = 0; i < 0x10; i++)
                    _kirk_buf[0x14 + i] ^= loc_1CF4[i];

                Array.Copy(_kirk_buf, 0x14, _key, 0, 0x10);
                //Array.Copy(_kirk_buf, 0x14, _header_key, 0, 0x10);

                if (_vkey != null)
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
            var offset = inputOffset;
            var outOffset = outputOffset;
            var size = inputCount;
            var seed = _seed;
            while (size > 0)
            {
                var dsize = Math.Min(0x800, size);

                var res = sub_428(inputBuffer, offset, dsize, outputBuffer, outOffset);
                if (res != 0) return inputCount - size;

                size -= dsize;
                offset += dsize;
                outOffset += dsize;
            }

            _seed = seed;
            return inputCount;
        }

        //TODO: Implement Encryption
        private int EncryptBuffer(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var offset = 0;
            var size = inputCount;
            var seed = _seed;
            while (size > 0)
            {
                var dsize = Math.Min(0x800, size);

                var res = sub_428(inputBuffer, offset, dsize, outputBuffer, outputOffset);
                if (res != 0) return inputCount - size;

                size -= dsize;
                offset += dsize;
            }

            _seed = seed;
            return inputCount;
        }

        private uint sub_428(byte[] buffer, int offset, int size, byte[] outputBuf, int outputOffset)
        {
            uint res;
            byte[] tmp1 = new byte[0x10];
            byte[] tmp2 = new byte[0x10];

            Array.Copy(_key, 0, _kirk_buf, 0x14, 0x10);

            for (int i = 0; i < 0x10; i++)
                _kirk_buf[0x14 + i] ^= loc_1CF4[i];

            if (_type == 2)
            {
                res = Kirk.DecryptWithFuse(_kirk_buf, 0x10);
            }
            else
            {
                res = Kirk.DecryptWith0(_kirk_buf, 0x10, 0x39);
            }
            if (res != 0) return res;

            for (int i = 0; i < 0x10; i++)
                _kirk_buf[i] ^= loc_1CE4[i];

            Array.Copy(_kirk_buf, tmp2, 0x10);

            if (_seed == 1)
            {
                Array.Clear(tmp1, 0, 0x10);
            }
            else
            {
                Array.Copy(tmp2, tmp1, 0x10);
                Array.Copy(BitConverter.GetBytes(_seed - 1), 0, tmp1, 0xC, 0x4);
            }

            for (int i = 0; i < size; i += 0x10)
            {
                Array.Copy(tmp2, 0, _kirk_buf, 0x14 + i, 0xC);
                Array.Copy(BitConverter.GetBytes(_seed), 0, _kirk_buf, 0x14 + i + 0xC, 0x4);
                _seed += 1;
            }

            res = sub_1F8(size, tmp1, 0x63);
            if (res != 0) return res;

            for (int i = 0; i < size; i++)
                outputBuf[outputOffset + i] = (byte)(buffer[offset + i] ^ _kirk_buf[i]);

            return 0;
        }

        private uint sub_1F8(int size, byte[] key, int key_type)
        {
            byte[] tmp = new byte[0x10];

            Array.Copy(_kirk_buf, 0x14 + size - 0x10, tmp, 0, 0x10);

            var res = Kirk.DecryptWith0(_kirk_buf, size, key_type);
            if (res != 0) return res;

            for (int i = 0; i < 0x10; i++)
                _kirk_buf[i] ^= key[i];

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