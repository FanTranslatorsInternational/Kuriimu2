using System;
using System.IO;

#if NET_CORE_31
using System.Buffers.Binary;
#endif

namespace Kryptography.Hash.Fnv
{
    public class Fnv1 : IHash
    {
        private const uint Initial = 0x811c9dc5;
        private const uint Prime = 0x1000193;

        public static Fnv1 Create()
        {
            return new Fnv1();
        }

        private Fnv1()
        {
        }

        public byte[] Compute(Span<byte> input)
        {
            var result = ComputeInternal(input, 0, input.Length, Initial);

            return MakeResult(result);
        }

        public byte[] Compute(Stream input)
        {
            var returnFnv = Initial;

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                returnFnv = ComputeInternal(buffer, 0, readSize, returnFnv);
            } while (readSize > 0);

            return MakeResult(returnFnv);
        }

        private uint ComputeInternal(Span<byte> toHash, int offset, int length, uint initialFnv)
        {
            var returnFnv = initialFnv;

            while (length-- > 0)
                returnFnv = (returnFnv * Prime) ^ toHash[offset++];

            return returnFnv;
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
