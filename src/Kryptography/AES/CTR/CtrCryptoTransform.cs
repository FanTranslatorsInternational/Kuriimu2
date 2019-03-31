using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Kryptography.AES.CTR
{
    public class CtrCryptoTransform : ICryptoTransform
    {
        public byte[] Ctr { get; set; }

        private ICryptoTransform _cryptor;
        private bool _littleEndianCtr;

        private bool _firstTransform = true;

        public CtrCryptoTransform(ICryptoTransform cryptor, byte[] ctr, bool littleEndianCtr)
        {
            Ctr = new byte[ctr.Length];
            Array.Copy(ctr, Ctr, ctr.Length);
            _cryptor = cryptor;
            _littleEndianCtr = littleEndianCtr;
        }

        public int InputBlockSize => _cryptor.InputBlockSize;

        public int OutputBlockSize => _cryptor.OutputBlockSize;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        public void Dispose()
        {
            _cryptor.Dispose();
            Ctr = null;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Validate();

            var ctrToUse = GetFirstToUseCtr(inputOffset);

            Process(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, ctrToUse);

            _firstTransform = false;
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            return outputBuffer;
        }

        private void Validate()
        {
            if (!_firstTransform && !CanReuseTransform)
                throw new NotSupportedException("Can't reuse transform.");
        }

        private byte[] GetFirstToUseCtr(int offset)
        {
            var result = new byte[InputBlockSize];
            Array.Copy(Ctr, result, InputBlockSize);

            var increase = offset / InputBlockSize;
            if (increase == 0) return result;

            result.Increment(increase, _littleEndianCtr);
            return result;
        }

        private void Process(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] ctr)
        {
            var alignedCount = RoundUpToMultiple(inputCount, InputBlockSize); // (int)Math.Ceiling((double)inputCount / InputBlockSize) * InputBlockSize;

            var encryptedCtrs = CreateXorPad(ctr, (int)alignedCount);

            XorData(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedCtrs);
        }

        private byte[] CreateXorPad(byte[] ctr, int alignedCount)
        {
            var ctrs = new byte[alignedCount];
            for (int i = 0; i < alignedCount; i += InputBlockSize)
            {
                Array.Copy(ctr, 0, ctrs, i, InputBlockSize);
                ctr.Increment(1, _littleEndianCtr);
            }
            _cryptor.TransformBlock(ctrs, 0, alignedCount, ctrs, 0);

            return ctrs;
        }

        private void XorData(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedCtrs)
        {
            var simdLength = Vector<byte>.Count;
            var j = 0;
            for (j = 0; j <= inputCount - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(inputBuffer, j + inputOffset);
                var vb = new Vector<byte>(encryptedCtrs, j);
                (va ^ vb).CopyTo(outputBuffer, j + outputOffset);
            }

            for (; j < inputCount; ++j)
            {
                outputBuffer[outputOffset + j] = (byte)(inputBuffer[inputOffset + j] ^ encryptedCtrs[j]);
            }
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