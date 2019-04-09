using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Kryptography.AES.XTS
{
    public class AesXtsCryptoTransform : ICryptoTransform
    {
        private ICryptoTransform _key1;
        private ICryptoTransform _key2;
        private readonly int _sectorSize;
        private readonly bool _advanceSectorId;
        private bool _firstTransform;
        private readonly bool _littleEndianId;

        public byte[] SectorId { get; set; }

        public AesXtsCryptoTransform(ICryptoTransform key1, ICryptoTransform key2, byte[] sectorId, bool advanceSectorId, int sectorSize, bool littleEndianId)
        {
            _key1 = key1;
            _key2 = key2;
            _sectorSize = sectorSize;
            _advanceSectorId = advanceSectorId;

            _firstTransform = true;
            _littleEndianId = littleEndianId;

            SectorId = new byte[sectorId.Length];
            Array.Copy(sectorId, SectorId, sectorId.Length);
        }

        public int InputBlockSize => 16;

        public int OutputBlockSize => 16;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        public void Dispose()
        {
            _key1.Dispose();
            _key1 = null;
            _key2.Dispose();
            _key2 = null;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Validate(inputCount);

            Process(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            _firstTransform = false;
            return inputCount;
        }

        private void Validate(int inputCount)
        {
            if (inputCount <= 0) throw new ArgumentOutOfRangeException(nameof(inputCount));
            if (!_firstTransform && !CanReuseTransform)
                throw new InvalidOperationException("Can't reuse transform.");
            if (inputCount % InputBlockSize > 0)
                throw new InvalidOperationException("Only aligned data can be processed.");
            if (_sectorSize % InputBlockSize > 0)
                throw new InvalidOperationException("SectorSize needs to be a multiple of 16.");
        }

        private void Process(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var tweak = new byte[SectorId.Length];
            Array.Copy(SectorId, tweak, tweak.Length);

            byte[] tweakPad = new byte[inputCount];
            ComputeTweakPad(tweak, tweakPad);

            ApplyXex(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, tweakPad);
        }

        private unsafe void ComputeTweakPad(byte[] tweak, byte[] tweakPad)
        {
            var inputCount = tweakPad.Length;
            var sectorCount = RoundUpToMultiple(inputCount, _sectorSize) / _sectorSize;

            var sectorTweaks = new byte[sectorCount << 4];
            for (int i = 0; i < sectorCount; i++)
            {
                Array.Copy(tweak, 0, sectorTweaks, i << 4, 16);
                if (_advanceSectorId)
                    tweak.Increment(1, _littleEndianId);
            }

            //Encrypt SectorTweaks
            _key2.TransformBlock(sectorTweaks, 0, sectorTweaks.Length, sectorTweaks, 0);

            fixed (byte* p = tweakPad)
            {
                long* q = (long*)p;
                for (int i = 0; i < sectorCount; i++)
                {
                    var q0 = BitConverter.ToInt64(sectorTweaks, i << 4);
                    var q1 = BitConverter.ToInt64(sectorTweaks, i << 4 | 0x8);

                    for (int j = 0; j < Math.Min(inputCount >> 4, _sectorSize >> 4); j++)
                    {
                        *q++ = q0;
                        *q++ = q1;
                        (q0, q1) = (q0 << 1 ^ (q1 >> 63 & 135), q1 << 1 ^ (q0 >> 63 & 1));  //Multiply by x
                    }
                    inputCount -= _sectorSize;
                }
            }
        }

        //private void TweakCrypt(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] tweak)
        //{
        //    var encryptedTweakBuffer = ArrayPool<byte>.Shared.Rent(inputCount);
        //    ComputeEncryptedTweaks(encryptedTweakBuffer, inputCount, tweak);

        //    ApplyXEX(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedTweakBuffer);

        //    ArrayPool<byte>.Shared.Return(encryptedTweakBuffer);
        //}

        //private unsafe void ComputeEncryptedTweaks(byte[] encryptedTweakBuffer, int inputCount, byte[] tweak)
        //{
        //    //Collect sector Tweaks
        //    var sectorCount = RoundUpToMultiple(inputCount, _sectorSize) / _sectorSize; // inputCount / SectorSize + (inputCount % SectorSize > 0 ? 1 : 0);
        //    var encTweak = ArrayPool<byte>.Shared.Rent(sectorCount << 4);
        //    for (int i = 0; i < sectorCount; i++)
        //    {
        //        Array.Copy(tweak, 0, encTweak, i * 16, 16);
        //        tweak.Increment(1, _littleEndianId);
        //    }

        //    //Encrypt SectorTweaks
        //    _key2.TransformBlock(encTweak, 0, encTweak.Length, encTweak, 0);

        //    fixed (byte* p = encryptedTweakBuffer)
        //    {
        //        long* q = (long*)p;
        //        for (int i = 0; i < sectorCount; i++)
        //        {
        //            var q0 = BitConverter.ToInt64(encTweak, i << 4);
        //            var q1 = BitConverter.ToInt64(encTweak, i << 4 | 0x8);

        //            for (int j = 0; j < Math.Min(inputCount >> 4, SectorSize >> 4); j++)
        //            {
        //                *q++ = q0;
        //                *q++ = q1;
        //                (q0, q1) = (q0 << 1 ^ (q1 >> 63 & 135), q1 << 1 ^ (q0 >> 63 & 1));  //Multiply by x
        //            }
        //            inputCount -= SectorSize;
        //        }
        //    }

        //    ArrayPool<byte>.Shared.Return(encTweak);
        //}

        private void ApplyXex(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedTweaks)
        {
            XorData(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedTweaks);
            _key1.TransformBlock(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset);
            XorData(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset, encryptedTweaks);
        }

        private void XorData(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedTweaks)
        {
            var simdLength = Vector<byte>.Count;
            int j;
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

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            return outputBuffer;
        }

        private long RoundUpToMultiple(long numToRound, int multiple)
        {
            if (multiple == 0)
                return numToRound;

            long remainder = numToRound % multiple;
            if (remainder == 0)
                return numToRound;

            return numToRound + multiple - remainder;
        }
    }
}