using System;
using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;

namespace Kryptography.AES.XTS
{
    public class XtsCryptoTransform : ICryptoTransform
    {
        private ICryptoTransform _key1;
        private ICryptoTransform _key2;
        public int SectorSize { get; set; }
        public byte[] SectorId { get; set; }
        private bool _firstTransform;
        private bool _littleEndianId;

        public XtsCryptoTransform(ICryptoTransform key1, ICryptoTransform key2, byte[] sectorId, int sectorSize, bool littleEndianId)
        {
            _key1 = key1;
            _key2 = key2;
            SectorSize = sectorSize;
            SectorId = sectorId;
            _firstTransform = true;
            _littleEndianId = littleEndianId;
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
            if (SectorSize % InputBlockSize > 0)
                throw new InvalidOperationException("SectorSize needs to be a multiple of 16.");
        }

        private void Process(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var tweak = new byte[16];
            Array.Copy(SectorId, tweak, 16);

            TweakCrypt(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, tweak);
        }

        private void TweakCrypt(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] tweak)
        {
            var encryptedTweakBuffer = ArrayPool<byte>.Shared.Rent(inputCount);
            ComputeEncryptedTweaks(encryptedTweakBuffer, inputCount, tweak);

            ApplyXEX(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedTweakBuffer);

            ArrayPool<byte>.Shared.Return(encryptedTweakBuffer);
        }

        private unsafe void ComputeEncryptedTweaks(byte[] encryptedTweakBuffer, int inputCount, byte[] tweak)
        {
            //Collect sector Tweaks
            var sectorCount = inputCount / SectorSize + (inputCount % SectorSize > 0 ? 1 : 0);
            var encTweak = ArrayPool<byte>.Shared.Rent(sectorCount << 4);
            for (int i = 0; i < sectorCount; i++)
            {
                Array.Copy(tweak, 0, encTweak, i * 16, 16);
                Increment(tweak, 1);
            }

            //Encrypt SectorTweaks
            _key2.TransformBlock(encTweak, 0, encTweak.Length, encTweak, 0);

            fixed (byte* p = encryptedTweakBuffer)
            {
                long* q = (long*)p;
                for (int i = 0; i < sectorCount; i++)
                {
                    var q0 = BitConverter.ToInt64(encTweak, i << 4);
                    var q1 = BitConverter.ToInt64(encTweak, i << 4 | 0x8);

                    for (int j = 0; j < Math.Min(inputCount >> 4, SectorSize >> 4); j++)
                    {
                        *q++ = q0;
                        *q++ = q1;
                        (q0, q1) = (q0 << 1 ^ (q1 >> 63 & 135), q1 << 1 ^ (q0 >> 63 & 1));  //Multiply by x
                    }
                    inputCount -= SectorSize;
                }
            }

            ArrayPool<byte>.Shared.Return(encTweak);
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

        private void Increment(byte[] id, int count)
        {
            if (!_littleEndianId)
                for (int i = id.Length - 1; i >= 0; i--)
                {
                    if (count == 0)
                        break;

                    var check = id[i];
                    id[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i - off - 1 >= 0 && id[i - off] < check)
                    {
                        check = id[i - off - 1];
                        id[i - off - 1]++;
                        off++;
                    }
                }
            else
                for (int i = 0; i < id.Length; i++)
                {
                    if (count == 0)
                        break;

                    var check = id[i];
                    id[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i + off + 1 < id.Length && id[i + off] < check)
                    {
                        check = id[i + off + 1];
                        id[i + off + 1]++;
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