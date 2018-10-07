using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.Cryptography.AES.CTR
{
    public class CtrTransform : ICryptoTransform
    {
        byte[] _key;
        byte[] _iv;
        ICryptoTransform _cryptor;

        bool _firstTransform = true;

        public CtrTransform(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;

            var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            _cryptor = aes.CreateEncryptor(key, null);
        }

        public int InputBlockSize => 16;

        public int OutputBlockSize => 16;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => false;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Validate();

            var ivToUse = GetFirstToUseIV(inputOffset);

            Process(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, ivToUse);

            _firstTransform = false;
            return inputCount;
        }

        private byte[] GetFirstToUseIV(int offset)
        {
            var increase = offset / InputBlockSize;
            if (increase == 0) return _iv;

            var buffer = new byte[InputBlockSize];
            Array.Copy(_iv, 0, buffer, 0, InputBlockSize);
            IncrementCtr(buffer, increase);

            return buffer;
        }

        private void IncrementCtr(byte[] ctr, int count)
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

            //for (int i = 0; i < count; i++)
            //    for (int j = ctr.Length - 1; j >= 0; j--)
            //        if (++ctr[j] != 0)
            //            break;
        }

        private void Process(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] iv)
        {
            var alignedCount = (int)Math.Ceiling((double)inputCount / InputBlockSize) * InputBlockSize;

            var encryptedIVs = GetProcessedIVs(iv, alignedCount);

            XORData(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset, encryptedIVs);
        }

        private void XORData(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, byte[] encryptedIVs)
        {
            var reducedCount = inputCount & ~0x7;
            var restCount = inputCount & 0x7;

            var inputSpan = MemoryMarshal.Cast<byte, long>(new Span<byte>(inputBuffer, inputOffset, reducedCount));
            var outputSpan = MemoryMarshal.Cast<byte, long>(new Span<byte>(outputBuffer, outputOffset, reducedCount));
            var ivSpan = MemoryMarshal.Cast<byte, long>(new Span<byte>(encryptedIVs, 0, reducedCount));
            for (int i = 0; i < reducedCount / 8; i++)
                outputSpan[i] = inputSpan[i] ^ ivSpan[i];

            for (int i = reducedCount; i < restCount; i++)
                outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ encryptedIVs[i]);
        }

        private byte[] GetProcessedIVs(byte[] initialIV, int alignedCount)
        {
            var ivs = new byte[alignedCount];
            for (int i = 0; i < alignedCount; i += InputBlockSize)
            {
                Array.Copy(initialIV, 0, ivs, i, InputBlockSize);
                IncrementCtr(initialIV, 1);
            }
            var encryptedIVs = new byte[alignedCount];
            _cryptor.TransformBlock(ivs, 0, alignedCount, encryptedIVs, 0);

            return encryptedIVs;
        }

        private void Validate()
        {
            if (!_firstTransform && !CanReuseTransform)
                throw new NotSupportedException("Can't reuse transform.");
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            return outputBuffer;
        }
    }
}
