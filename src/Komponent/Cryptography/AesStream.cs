using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using Komponent.Cryptography.AES;
using Komponent.Cryptography.AES.XTS;
using Kontract.Interfaces;

namespace Komponent.Cryptography
{
    public enum AesMode : int
    {
        ECB,
        CBC,
        //CFB,
        //CTS,
        //OFB,

        CTR,
        XTS
    }

    public class AesStream : IKryptoStream
    {
        public int BlockSize { get; private set; }

        public byte[] Key => throw new NotImplementedException();

        public int KeySize => throw new NotImplementedException();

        public byte[] IV => throw new NotImplementedException();

        CryptoStream _decryptor = null;
        CryptoStream _encryptor = null;

        CtrCryptoTransform _ctrDecryptor = null;
        CtrCryptoTransform _ctrEncryptor = null;

        XtsStream _xtsDecryptor = null;
        XtsStream _xtsEncryptor = null;


        Stream _stream;
        AesMode _aesMode;

        public AesStream(Stream input, byte[] key, AesMode aesMode) : this(input, key, null, null, aesMode)
        {
        }

        public AesStream(Stream input, byte[] key, byte[] iv, AesMode aesMode) : this(input, key, null, iv, aesMode)
        {
        }

        public AesStream(Stream input, byte[] key, AesMode aesMode, bool xtsNinTweak) : this(input, key, null, null, aesMode, xtsNinTweak)
        {
        }

        public AesStream(Stream input, byte[] key, byte[] iv, AesMode aesMode, bool xtsNinTweak) : this(input, key, null, iv, aesMode, xtsNinTweak)
        {
        }

        public AesStream(Stream input, byte[] key1, byte[] key2, byte[] iv, AesMode aesMode, bool xtsNinTweak = false)
        {
            _stream = input;

            if (Enum.TryParse<CipherMode>(aesMode.ToString(), out var mode))
            {
                AesManaged aes;

                if (iv != null)
                {
                    aes = new AesManaged
                    {
                        Key = key1,
                        IV = iv,
                        Mode = mode
                    };
                }
                else
                {
                    aes = new AesManaged
                    {
                        Key = key1,
                        Mode = mode
                    };
                }

                _decryptor = new CryptoStream(_stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                _encryptor = new CryptoStream(_stream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            }
            else
            {
                switch (aesMode)
                {
                    case AesMode.CTR:
                        var ctr = new CTR(iv);

                        _ctrDecryptor = ctr.CreateDecryptor(key1) as CtrCryptoTransform;
                        _ctrEncryptor = ctr.CreateEncryptor(key1) as CtrCryptoTransform;

                        _decryptor = new CryptoStream(_stream, _ctrDecryptor, CryptoStreamMode.Read);
                        _encryptor = new CryptoStream(_stream, _ctrEncryptor, CryptoStreamMode.Write);
                        break;
                    case AesMode.XTS:
                        if (key2 != null)
                        {
                            if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                            {
                                _xtsDecryptor = new XtsStream(_stream, XtsAes128.Create(key1, key2, xtsNinTweak));
                                _xtsEncryptor = new XtsStream(_stream, XtsAes128.Create(key1, key2, xtsNinTweak));
                            }
                            else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                            {
                                _xtsDecryptor = new XtsStream(_stream, XtsAes256.Create(key1, key2, xtsNinTweak));
                                _xtsEncryptor = new XtsStream(_stream, XtsAes256.Create(key1, key2, xtsNinTweak));
                            }
                            else
                                throw new InvalidDataException("Key1 or Key2 have invalid size.");
                        }
                        else
                        {
                            if (key1.Length == 256 / 8)
                            {
                                _xtsDecryptor = new XtsStream(_stream, XtsAes128.Create(key1, xtsNinTweak));
                                _xtsEncryptor = new XtsStream(_stream, XtsAes128.Create(key1, xtsNinTweak));
                            }
                            else if (key1.Length == 512 / 8)
                            {
                                _xtsDecryptor = new XtsStream(_stream, XtsAes256.Create(key1, xtsNinTweak));
                                _xtsEncryptor = new XtsStream(_stream, XtsAes256.Create(key1, xtsNinTweak));
                            }
                            else
                                throw new InvalidDataException("Key1 has invalid size.");
                        }
                        break;
                }
            }

            _aesMode = aesMode;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_aesMode == AesMode.XTS)
                return _xtsDecryptor.Read(buffer, offset, count);
            else
                return _decryptor.Read(buffer, offset, count);
        }

        public int ReadByte()
        {
            if (_aesMode == AesMode.XTS)
                return _xtsDecryptor.ReadByte();
            else
                return _decryptor.ReadByte();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            if (_aesMode == AesMode.CTR)
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _ctrDecryptor.SeekCtr(offset);
                        _ctrEncryptor.SeekCtr(offset);
                        break;
                    case SeekOrigin.Current:
                        _ctrDecryptor.SeekCtr(_stream.Position + offset);
                        _ctrEncryptor.SeekCtr(_stream.Position + offset);
                        break;
                    case SeekOrigin.End:
                        _ctrDecryptor.SeekCtr(_stream.Length + offset);
                        _ctrEncryptor.SeekCtr(_stream.Length + offset);
                        break;
                }

            return _stream.Seek(offset, origin);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_aesMode == AesMode.XTS)
            {
                _xtsEncryptor.Write(buffer, offset, count);
                _xtsEncryptor.Flush();
            }
            else
            {
                _encryptor.Write(buffer, offset, count);
                _encryptor.Flush();
            }
        }

        public void WriteByte(byte value)
        {
            if (_aesMode == AesMode.XTS)
            {
                _xtsEncryptor.WriteByte(value);
                _xtsEncryptor.Flush();
            }
            else
            {
                _encryptor.WriteByte(value);
                _encryptor.Flush();
            }
        }
    }
}
