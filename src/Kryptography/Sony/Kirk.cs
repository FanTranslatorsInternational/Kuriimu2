using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Kryptography.Sony
{
    public class Kirk
    {
        static Aes aes = Aes.Create();

        /// <summary>
        /// Kirk CMD4
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <param name="key_type"></param>
        /// <returns></returns>
        public static uint EncryptWith0(byte[] buffer, int size, int key_type)
        {
            WriteUInt32ToByteArray(buffer, 0, 4);
            WriteUInt32ToByteArray(buffer, 4, 0);
            WriteUInt32ToByteArray(buffer, 8, 0);
            WriteUInt32ToByteArray(buffer, 0xC, (uint)key_type);
            WriteUInt32ToByteArray(buffer, 0x10, (uint)size);

            var res = BufferCopyWithRange(buffer, size + 0x14, buffer, size, (int)KirkCmd.Encrypt_0);
            if (res > 0) return 0x80510311;

            return 0;
        }

        /// <summary>
        /// Kirk CMD5
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static uint EncryptWithFuse(byte[] buffer, int size)
        {
            WriteUInt32ToByteArray(buffer, 0, 4);
            WriteUInt32ToByteArray(buffer, 4, 0);
            WriteUInt32ToByteArray(buffer, 8, 0);
            WriteUInt32ToByteArray(buffer, 0xC, 0x100);
            WriteUInt32ToByteArray(buffer, 0x10, (uint)size);

            var res = BufferCopyWithRange(buffer, size + 0x14, buffer, size, (int)KirkCmd.Encrypt_Fuse);
            if (res > 0) return 0x80510312;

            return 0;
        }

        /// <summary>
        /// Kirk CMD7
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <param name="key_type"></param>
        /// <returns></returns>
        public static uint DecryptWith0(byte[] buffer, int size, int key_type)
        {
            WriteUInt32ToByteArray(buffer, 0, 5);
            WriteUInt32ToByteArray(buffer, 4, 0);
            WriteUInt32ToByteArray(buffer, 8, 0);
            WriteUInt32ToByteArray(buffer, 0xC, (uint)key_type);
            WriteUInt32ToByteArray(buffer, 0x10, (uint)size);

            var res = BufferCopyWithRange(buffer, size + 0x14, buffer, size, (int)KirkCmd.Decrypt_0);
            if (res > 0) return 0x80510311;

            return 0;
        }

        /// <summary>
        /// Kirk CMD8
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static uint DecryptWithFuse(byte[] buffer, int size)
        {
            WriteUInt32ToByteArray(buffer, 0, 5);
            WriteUInt32ToByteArray(buffer, 4, 0);
            WriteUInt32ToByteArray(buffer, 8, 0);
            WriteUInt32ToByteArray(buffer, 0xC, 0x100);
            WriteUInt32ToByteArray(buffer, 0x10, (uint)size);

            var res = BufferCopyWithRange(buffer, size + 0x14, buffer, size, (int)KirkCmd.Decrypt_Fuse);
            if (res > 0) return 0x80510312;

            return 0;
        }

        public static uint CryptKirk14(byte[] buffer)
        {
            var res = BufferCopyWithRange(buffer, 0x14, new byte[0], 0, 14);
            if (res > 0) return 0x80510315;

            return 0;
        }

        private static void WriteUInt32ToByteArray(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
        private static uint ReadUInt32FromByteArray(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
        }

        private static int BufferCopyWithRange(byte[] outBuf, int outSize, byte[] inBuf, int inSize, int cmd)
        {
            switch (cmd)
            {
                case 4:
                    return Kirk_CMD4(outBuf, inBuf, inSize);
                case 7:
                    return Kirk_CMD7(outBuf, inBuf, inSize);
                case 14:
                    //TODO: Implement random byte filler CMD14
                    //Array.Clear(outBuf, 0, outSize);
                    return 0;
                default:
                    throw new InvalidOperationException($"Invalid Command: {(KirkCmd)cmd}");
            }
        }

        private enum KirkCmd : int
        {
            Decrypt_Private = 1,
            Encrypt_0 = 4,
            Encrypt_Fuse = 5,
            Encrypt_User = 6,
            Decrypt_0 = 7,
            Decrypt_Fuse = 8,
            Decrypt_User = 9,
            Priv_Sig_Check = 10,
            SHA1 = 11
        }

        //Actual encryption method; AES128 CBC
        private static int Kirk_CMD4(byte[] outBuf, byte[] inBuf, int size)
        {
            var mode = ReadUInt32FromByteArray(inBuf, 0);
            var key_seed = ReadUInt32FromByteArray(inBuf, 0xC);
            var data_size = ReadUInt32FromByteArray(inBuf, 0x10);

            if (mode != 4)
                return 2;
            if (data_size == 0)
                return 0x10;

            var key = GetKirk47Key(key_seed);
            if (key == null) return 0xF;    //should it be 0xE for invalid seed? kirk_engine.c L291

            //Encrypt inBuf
            CBCEncrypt(outBuf, inBuf, (int)data_size, key, new byte[0x10]);

            Array.Copy(inBuf, 0, outBuf, 0, 0x14);
            WriteUInt32ToByteArray(outBuf, 0, 5);
            return 0;
        }

        private static void CBCEncrypt(byte[] outBuf, byte[] inBuf, int size, byte[] key, byte[] iv)
        {
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.IV = iv;

            var cryptor = aes.CreateEncryptor();
            cryptor.TransformBlock(inBuf, 0x14, size, outBuf, 0x14);
        }

        //Actual decryption method; AES128 CBC
        private static int Kirk_CMD7(byte[] outBuf, byte[] inBuf, int size)
        {
            var mode = ReadUInt32FromByteArray(inBuf, 0);
            var key_seed = ReadUInt32FromByteArray(inBuf, 0xC);
            var data_size = ReadUInt32FromByteArray(inBuf, 0x10);

            if (mode != 5)
                return 2;
            if (data_size == 0)
                return 0x10;

            var key = GetKirk47Key(key_seed);
            if (key == null) return 0xF;    //should it be 0xE for invalid seed? kirk_engine.c L291

            //Decrypt inBuf
            CBCDecrypt(outBuf, inBuf, (int)data_size, key, new byte[0x10]);

            return 0;
        }

        private static void CBCDecrypt(byte[] outBuf, byte[] inBuf, int size, byte[] key, byte[] iv)
        {
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.IV = iv;

            var cryptor = aes.CreateDecryptor();
            cryptor.TransformBlock(inBuf, 0x14, size, outBuf, 0);
        }

        private static byte[] GetKirk47Key(uint seed)
        {
            if (!_kirk47Keys.ContainsKey(seed))
                return null;

            return _kirk47Keys[seed];
        }

        private static Dictionary<uint, byte[]> _kirk47Keys = new Dictionary<uint, byte[]>
        {
            [0x03] = new byte[] { 0x98, 0x02, 0xC4, 0xE6, 0xEC, 0x9E, 0x9E, 0x2F, 0xFC, 0x63, 0x4C, 0xE4, 0x2F, 0xBB, 0x46, 0x68 },
            [0x04] = new byte[] { 0x99, 0x24, 0x4C, 0xD2, 0x58, 0xF5, 0x1B, 0xCB, 0xB0, 0x61, 0x9C, 0xA7, 0x38, 0x30, 0x07, 0x5F },
            [0x05] = new byte[] { 0x02, 0x25, 0xD7, 0xBA, 0x63, 0xEC, 0xB9, 0x4A, 0x9D, 0x23, 0x76, 0x01, 0xB3, 0xF6, 0xAC, 0x17 },
            [0x0C] = new byte[] { 0x84, 0x85, 0xC8, 0x48, 0x75, 0x08, 0x43, 0xBC, 0x9B, 0x9A, 0xEC, 0xA7, 0x9C, 0x7F, 0x60, 0x18 },
            [0x0D] = new byte[] { 0xB5, 0xB1, 0x6E, 0xDE, 0x23, 0xA9, 0x7B, 0x0E, 0xA1, 0x7C, 0xDB, 0xA2, 0xDC, 0xDE, 0xC4, 0x6E },
            [0x0E] = new byte[] { 0xC8, 0x71, 0xFD, 0xB3, 0xBC, 0xC5, 0xD2, 0xF2, 0xE2, 0xD7, 0x72, 0x9D, 0xDF, 0x82, 0x68, 0x82 },
            [0x0F] = new byte[] { 0x0A, 0xBB, 0x33, 0x6C, 0x96, 0xD4, 0xCD, 0xD8, 0xCB, 0x5F, 0x4B, 0xE0, 0xBA, 0xDB, 0x9E, 0x03 },
            [0x10] = new byte[] { 0x32, 0x29, 0x5B, 0xD5, 0xEA, 0xF7, 0xA3, 0x42, 0x16, 0xC8, 0x8E, 0x48, 0xFF, 0x50, 0xD3, 0x71 },
            [0x11] = new byte[] { 0x46, 0xF2, 0x5E, 0x8E, 0x4D, 0x2A, 0xA5, 0x40, 0x73, 0x0B, 0xC4, 0x6E, 0x47, 0xEE, 0x6F, 0x0A },
            [0x12] = new byte[] { 0x5D, 0xC7, 0x11, 0x39, 0xD0, 0x19, 0x38, 0xBC, 0x02, 0x7F, 0xDD, 0xDC, 0xB0, 0x83, 0x7D, 0x9D },
            [0x38] = new byte[] { 0x12, 0x46, 0x8D, 0x7E, 0x1C, 0x42, 0x20, 0x9B, 0xBA, 0x54, 0x26, 0x83, 0x5E, 0xB0, 0x33, 0x03 },
            [0x39] = new byte[] { 0xC4, 0x3B, 0xB6, 0xD6, 0x53, 0xEE, 0x67, 0x49, 0x3E, 0xA9, 0x5F, 0xBC, 0x0C, 0xED, 0x6F, 0x8A },
            [0x3A] = new byte[] { 0x2C, 0xC3, 0xCF, 0x8C, 0x28, 0x78, 0xA5, 0xA6, 0x63, 0xE2, 0xAF, 0x2D, 0x71, 0x5E, 0x86, 0xBA },
            [0x4B] = new byte[] { 0x0C, 0xFD, 0x67, 0x9A, 0xF9, 0xB4, 0x72, 0x4F, 0xD7, 0x8D, 0xD6, 0xE9, 0x96, 0x42, 0x28, 0x8B }, //1.xx game eboot.bin
            [0x53] = new byte[] { 0xAF, 0xFE, 0x8E, 0xB1, 0x3D, 0xD1, 0x7E, 0xD8, 0x0A, 0x61, 0x24, 0x1C, 0x95, 0x92, 0x56, 0xB6 },
            [0x57] = new byte[] { 0x1C, 0x9B, 0xC4, 0x90, 0xE3, 0x06, 0x64, 0x81, 0xFA, 0x59, 0xFD, 0xB6, 0x00, 0xBB, 0x28, 0x70 },
            [0x5D] = new byte[] { 0x11, 0x5A, 0x5D, 0x20, 0xD5, 0x3A, 0x8D, 0xD3, 0x9C, 0xC5, 0xAF, 0x41, 0x0F, 0x0F, 0x18, 0x6F },
            [0x63] = new byte[] { 0x9C, 0x9B, 0x13, 0x72, 0xF8, 0xC6, 0x40, 0xCF, 0x1C, 0x62, 0xF5, 0xD5, 0x92, 0xDD, 0xB5, 0x82 },
            [0x64] = new byte[] { 0x03, 0xB3, 0x02, 0xE8, 0x5F, 0xF3, 0x81, 0xB1, 0x3B, 0x8D, 0xAA, 0x2A, 0x90, 0xFF, 0x5E, 0x61 },
        };
    }
}
