namespace Kryptography.Encryption.Sony
{
    public class BbMac
    {
        private readonly byte[] _kirkBuf = new byte[0x0814];
        private readonly byte[] _loc1Cd4 = [0xE3, 0x50, 0xED, 0x1D, 0x91, 0x0A, 0x1F, 0xD0, 0x29, 0xBB, 0x1C, 0x3E, 0xF3, 0x40, 0x77, 0xFB];

        private int _macType;
        private int _padSize;

        private readonly byte[] _key = new byte[0x10];
        private readonly byte[] _pad = new byte[0x10];

        /// <summary>
        /// Initializes the BBMac context
        /// </summary>
        /// <param name="macType">Type of MAC</param>
        public BbMac(int macType)
        {
            _macType = macType;
            _padSize = 0;

            Array.Clear(_key, 0, 0x10);
            Array.Clear(_pad, 0, 0x10);
        }

        /// <summary>
        /// Validate a range of bytes with a given MAC
        /// </summary>
        /// <param name="bbmac"></param>
        /// <param name="macKey"></param>
        /// <param name="range"></param>
        public bool Validate(byte[] range, byte[] bbmac, byte[]? macKey)
        {
            var result = Update(range, range.Length);
            if (result != 0) return false;

            result = Final(bbmac, macKey);
            if (result != 0) return false;

            return true;
        }

        /// <summary>
        /// Gets MAC Key from MAC and range of bytes
        /// </summary>
        /// <param name="range"></param>
        /// <param name="bbmac"></param>
        /// <returns></returns>
        public byte[]? GetKey(byte[] range, byte[] bbmac)
        {
            var result = Update(range, range.Length);
            if (result != 0) return null;

            var tmp = new byte[0x10];
            var tmp1 = new byte[0x10];

            var type = _macType;
            var res = PrivateFinal(tmp, null);
            if (res != 0) return null;

            //decrypt bbmac
            if (type == 3)
            {
                Array.Copy(bbmac, 0, _kirkBuf, 0x14, 0x10);
                Kirk.DecryptWith0(_kirkBuf, 0x10, 0x63);
            }
            else
            {
                Array.Copy(bbmac, 0, _kirkBuf, 0, 0x10);
            }

            Array.Copy(_kirkBuf, 0, tmp1, 0, 0x10);
            Array.Copy(tmp1, 0, _kirkBuf, 0x14, 0x10);

            var code = type == 2 ? 0x3A : 0x38;
            Kirk.DecryptWith0(_kirkBuf, 0x10, code);

            var vkey = new byte[0x10];
            for (int i = 0; i < 0x10; i++)
                vkey[i] = (byte)(tmp[i] ^ _kirkBuf[i]);

            return vkey;
        }

        /// <summary>
        /// Updates the BBMac key in the context
        /// </summary>
        /// <param name="buffer">Buffer to update from</param>
        /// <param name="size"></param>
        /// <returns></returns>
        private long Update(byte[] buffer, int size)
        {
            if (_padSize > 16)
                return 0x80510302;

            if (_padSize + size <= 16)
            {
                Array.Copy(buffer, 0, _pad, _padSize, size);
                _padSize += size;

                return 0;
            }

            Array.Copy(_pad, 0, _kirkBuf, 0x14, _padSize);

            var p = _padSize;
            _padSize += size;
            _padSize &= 0xF;
            if (_padSize == 0) _padSize = 0x10;

            size -= _padSize;
            Array.Copy(buffer, size, _pad, 0, _padSize);

            int type = _macType == 2 ? 0x3A : 0x38;

            int bufferPos = 0;
            while (size > 0)
            {
                var ksize = Math.Min(0x800, size + p);
                Array.Copy(buffer, bufferPos, _kirkBuf, 0x14 + p, ksize - p);

                var res = EncryptBuffer(_kirkBuf, ksize, _key, type);
                if (res != 0) return res;

                size -= ksize - p;
                bufferPos += ksize - p;
                p = 0;
            }

            return 0;
        }

        /// <summary>
        /// Validates MAC
        /// </summary>
        /// <param name="outBuf"></param>
        /// <param name="vkey"></param>
        /// <returns>0, if MAC is valid</returns>
        private long Final(byte[] outBuf, byte[]? vkey)
        {
            var tmp = new byte[0x10];

            var type = _macType;
            var res = PrivateFinal(tmp, vkey);
            if (res != 0) return res;

            //decrypt bbmac
            if (type == 3)
            {
                Array.Copy(outBuf, 0, _kirkBuf, 0x14, 0x10);
                Kirk.DecryptWith0(_kirkBuf, 0x10, 0x63);
            }
            else
            {
                Array.Copy(outBuf, 0, _kirkBuf, 0, 0x10);
            }

            for (int i = 0; i < 0x10; i++)
                if (_kirkBuf[i] != tmp[i])
                    return 0x80510300;

            return 0;
        }

        private long PrivateFinal(byte[] buffer, byte[]? vkey)
        {
            byte[] tmp = new byte[0x10];
            byte[] tmp1 = new byte[0x10];

            if (_padSize > 16)
                return 0x80510302;

            var code = _macType == 2 ? 0x3A : 0x38;

            Array.Clear(_kirkBuf, 0x14, 0x10);
            long res = Kirk.EncryptWith0(_kirkBuf, 0x10, code);
            if (res != 0) return res;

            Array.Copy(_kirkBuf, 0x14, tmp, 0, 0x10);

            MultiplyByX(tmp);
            if (_padSize < 16)
            {
                MultiplyByX(tmp);

                _pad[_padSize] = 0x80;
                if (_padSize + 1 < 16)
                    Array.Clear(_pad, _padSize + 1, 16 - _padSize - 1);
            }

            for (int i = 0; i < 16; i++)
                _pad[i] ^= tmp[i];

            Array.Copy(_pad, 0, _kirkBuf, 0x14, 0x10);
            Array.Copy(_key, 0, tmp1, 0, 0x10);

            res = EncryptBuffer(_kirkBuf, 0x10, tmp1, code);
            if (res != 0) return res;

            for (int i = 0; i < 0x10; i++)
                tmp1[i] ^= _loc1Cd4[i];

            if (_macType == 2)
            {
                Array.Copy(tmp1, 0, _kirkBuf, 0x14, 0x10);

                res = Kirk.EncryptWithFuse(_kirkBuf, 0x10);
                if (res != 0) return res;

                res = Kirk.EncryptWith0(_kirkBuf, 0x10, code);
                if (res != 0) return res;

                Array.Copy(_kirkBuf, 0x14, tmp1, 0, 0x10);
            }

            if (vkey != null)
            {
                for (int i = 0; i < 0x10; i++)
                    tmp1[i] ^= vkey[i];
                Array.Copy(tmp1, 0, _kirkBuf, 0x14, 0x10);

                res = Kirk.EncryptWith0(_kirkBuf, 0x10, code);
                if (res != 0) return res;

                Array.Copy(_kirkBuf, 0x14, tmp1, 0, 0x10);
            }

            Array.Copy(tmp1, 0, buffer, 0, 0x10);

            Array.Clear(_key, 0, 0x10);
            Array.Clear(_pad, 0, 0x10);

            _padSize = 0;
            _macType = 0;

            return 0;
        }

        private static void MultiplyByX(byte[] toMult)
        {
            byte t0 = (byte)(toMult[0] >> 7 == 1 ? 0x87 : 0);
            for (int i = 0; i < 15; i++)
            {
                var v1 = toMult[i];
                var v0 = toMult[i + 1];
                v1 <<= 1;
                v0 >>= 7;
                v0 |= v1;
                toMult[i] = v0;
            }

            var v2 = toMult[15];
            v2 <<= 1;
            v2 ^= t0;
            toMult[15] = v2;
        }

        private static long EncryptBuffer(byte[] buffer, int size, byte[] key, int keyType)
        {
            for (int i = 0; i < 16; i++)
                buffer[0x14 + i] ^= key[i];

            var res = Kirk.EncryptWith0(buffer, size, keyType);
            if (res != 0) return res;

            Array.Copy(buffer, size + 4, key, 0, 0x10);

            return 0;
        }
    }
}