using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Kryptography.Sony
{
    public class BBCipherContext : SymmetricAlgorithm
    {
        byte[] _header_key;
        byte[] _vkey;

        int _type;
        int _seed;

        public BBCipherContext(byte[] header_key, byte[] vkey, int seed, int cipher_type)
        {
            _header_key = new byte[header_key.Length];
            Array.Copy(header_key, _header_key, header_key.Length);

            _vkey = new byte[vkey.Length];
            Array.Copy(vkey, _vkey, vkey.Length);

            _type = cipher_type;
            _seed = seed;
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(null, null);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new BBCipherTransform(_header_key, _vkey, _seed, _type, true);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(null, null);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new BBCipherTransform(_header_key, _vkey, _seed, _type, false);
        }

        public override void GenerateIV()
        {
            throw new NotImplementedException();
        }

        public override void GenerateKey()
        {
            throw new NotImplementedException();
        }
    }
}
