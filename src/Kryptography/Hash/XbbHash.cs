using System;
using System.IO;

#if NET_CORE_31
using System.Buffers.Binary;
#endif

namespace Kryptography.Hash
{
    public class XbbHash : IHash
    {
        public byte[] Compute(Span<byte> input)
        {
            var seed = 0;
            var returnValue = ComputeInternal(input, 0, input.Length, 0, ref seed);

            return MakeResult(returnValue);
        }

        public byte[] Compute(Stream input)
        {
            var returnValue = 0u;
            var seed = 0;

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                returnValue = ComputeInternal(buffer, 0, readSize, returnValue, ref seed);
            } while (readSize > 0);

            return MakeResult(returnValue);
        }

        private uint ComputeInternal(Span<byte> toHash, int offset, int length, uint initialValue, ref int seed)
        {
            var result = initialValue;
            for (var i = offset; i < offset + length; i++)
            {
                var c = toHash[i];

                seed += c;
                result += (uint)((c << seed) | c >> -seed);
            }

            return result;
        }

        private byte[] MakeResult(uint result)
        {
            var returnBuffer = new byte[4];

#if NET_CORE_31
            BinaryPrimitives.WriteUInt32BigEndian(returnBuffer, result);
#else
            returnBuffer[0] = (byte)(result >> 24);
            returnBuffer[1] = (byte)(result >> 16);
            returnBuffer[2] = (byte)(result >> 8);
            returnBuffer[3] = (byte)result;
#endif

            return returnBuffer;
        }
    }
}
