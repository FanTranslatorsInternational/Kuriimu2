using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Kontract.Encryption.AES.XTS;
using Kontract.Encryption.AES.CTR;

namespace Kontract.Encryption.AES
{
    public class Decryption
    {
        public static byte[] XTS128(byte[] block, byte[] key, int block_size, bool _nin_tweak = false, int section_id = 0)
        {
            var result = new byte[block.Length];

            var xts = XtsAes128.Create(key, _nin_tweak);
            using (var transform = xts.CreateDecryptor())
            {
                for (int i = 0; i < block.Length / block_size; i++)
                {
                    transform.TransformBlock(block, i * block_size, block_size, result, i * block_size, Convert.ToUInt64(section_id++));
                }
            }

            return result;
        }

        public static byte[] ECB128(byte[] block, byte[] key)
        {
            var aes = Aes.Create();

            aes.BlockSize = 128;
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            var output = new byte[block.Length];
            aes.CreateDecryptor().TransformBlock(block, 0, block.Length, output, 0);
            return output;
        }

        public static byte[] CBC128(byte[] block, byte[] key, byte[] iv)
        {
            var aes = Aes.Create();

            aes.BlockSize = 128;
            aes.IV = iv;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var output = new byte[block.Length];
            aes.CreateDecryptor().TransformBlock(block, 0, block.Length, output, 0);
            return output;
        }

        public static byte[] CTR128(byte[] block, byte[] key, byte[] ctr)
        {
            var aes = new AesCtr(ctr);

            var output = new byte[block.Length];
            aes.CreateDecryptor(key).TransformBlock(block, 0, block.Length, output, 0);
            return output;
        }
    }
}
