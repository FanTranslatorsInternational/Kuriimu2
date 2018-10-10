using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.Cryptography.AES.XTS
{
    public class XtsCryptoTransform : ICryptoTransform
    {
        private ICryptoTransform _key1;
        private ICryptoTransform _key2;
        public int SectorSize { get; set; }
        public byte[] SectorId { get; set; }
        private bool _firstTransform;

        public XtsCryptoTransform(ICryptoTransform key1, ICryptoTransform key2, byte[] sectorId, int sectorSize, bool decrypting)
        {
            _key1 = key1;
            _key2 = key2;
            SectorSize = sectorSize;
            SectorId = sectorId;
            _firstTransform = true;
        }

        public int InputBlockSize => 16;

        public int OutputBlockSize => 16;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        public void Dispose()
        {
            _key1 = null;
            _key2 = null;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Validate(inputCount);

            Process(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            _firstTransform = false;
            return 0;
        }

        private void Validate(int inputCount)
        {
            if (!_firstTransform && !CanReuseTransform)
                throw new InvalidOperationException("Can't reuse transform.");
            if (inputCount % InputBlockSize != 0)
                throw new InvalidOperationException("Only aligned data can be encrypted.");
        }
        
        private void Process(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            //Array.Copy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);

            var tweak = new byte[16];
            //var encTweak = new byte[16];
            Array.Copy(SectorId, tweak, 16);

            for (int i = 0; i < Math.Ceiling((double)inputCount / SectorSize); i++)
            {
                var length = inputCount % SectorSize != 0 ? inputCount % SectorSize : SectorSize;
                TweakCryptSector(inputBuffer, inputOffset, length, outputBuffer, outputOffset, tweak);

                //Increment tweak
                Increment(tweak, 1);
            }
        }

        private void TweakCryptSector(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] tweak)
        {
            //Encrypt tweaks for sector
            byte[] encryptedTweaks = InitializeByteArray(inputCount);
            ComputeEncryptedTweaks(encryptedTweaks, inputCount, tweak);

            //XEX
            ApplyXEX(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedTweaks);

            //Free encryptedTweaks
            FreeByteArray(encryptedTweaks);
        }

        private byte[] InitializeByteArray(int length)
        {
            if (length <= 0x40)
                return new byte[length];
            else
                return ArrayPool<byte>.Shared.Rent(length);
        }

        private unsafe void ComputeEncryptedTweaks(byte[] encryptedTweakBuffer, int inputCount, byte[] tweak)
        {
            //var encTweak = new byte[16];
            //_key2.TransformBlock(tweak, 0, 16, encTweak, 0);

            ////Compute encrypted tweaks for every block
            //for (int i = 0; i < inputCount >> 4; i++)
            //{
            //    Buffer.BlockCopy(encTweak, 0, encryptedTweakBuffer, i * 16, 16);
            //    MultiplyByX(encTweak);
            //}

            var encTweak = new byte[16]; // todo: compare with ArrayPool
            _key2.TransformBlock(tweak, 0, 16, encTweak, 0);

            var q0 = BitConverter.ToInt64(encTweak, 0);
            var q1 = BitConverter.ToInt64(encTweak, 8);

            fixed (byte* p = encryptedTweakBuffer)
            {
                long* q = (long*)p;
                for (int i = 0; i < inputCount >> 4; i++)
                {
                    *q++ = q0;
                    *q++ = q1;
                    (q0, q1) = (q0 << 1 ^ (q1 >> 63 & 135), q1 << 1 ^ (q0 >> 63 & 1));
                }
            }
        }

        private void ApplyXEX(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedTweaks)
        {
            XORData(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedTweaks);
            _key1.TransformBlock(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset);
            XORData(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset, encryptedTweaks);
        }

        private void XORData(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedTweaks)
        {
            var simdLength = Vector<byte>.Count;
            var j = 0;
            for (j = 0; j <= inputCount - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(inputBuffer, j + inputOffset);
                var vb = new Vector<byte>(encryptedTweaks, j);
                (va ^ vb).CopyTo(outputBuffer, j + outputOffset);
            }

            for (; j < inputCount; ++j)
            {
                outputBuffer[outputOffset + j] = (byte)(inputBuffer[inputOffset + j] ^ encryptedTweaks[j]);
            }
        }

        private void FreeByteArray(byte[] toFree)
        {
            if (toFree.Length <= 0x40)
                toFree = null;
            else
                ArrayPool<byte>.Shared.Return(toFree);
        }

        private void MultiplyByX(byte[] i)
        {
            byte t = 0, tt = 0;

            for (var x = 0; x < 16; x++)
            {
                tt = (byte)(i[x] >> 7);
                i[x] = (byte)(((i[x] << 1) | t) & 0xFF);
                t = tt;
            }

            if (tt > 0)
                i[0] ^= 0x87;
        }

        private void Increment(byte[] ctr, int count)
        {
            for (int i = ctr.Length - 1; i >= 0; i--)
            {
                if (count == 0)
                    break;

                var check = ctr[i];
                ctr[i] += (byte)count;
                count >>= 8;

                int off = 0;
                while (i - off - 1 >= 0 && ctr[i - off] < check)
                {
                    check = ctr[i - off - 1];
                    ctr[i - off - 1]++;
                    off++;
                }
            }
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            return outputBuffer;
        }
    }
}
