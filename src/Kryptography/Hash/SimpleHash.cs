using System;
using System.IO;
using System.Linq;

#if NET_CORE_31
using System.Buffers.Binary;
#endif

namespace Kryptography.Hash
{
    public class SimpleHash:IHash
    {
        private readonly uint _magic;

        public SimpleHash(uint magic)
        {
            _magic = magic;
        }

        public byte[] Compute(Span<byte> input)
        {
            var returnValue = ComputeInternal(input, 0, input.Length, 0);

            return MakeResult(returnValue);
        }

        public byte[] Compute(Stream input)
        {
            var returnValue = 0u;

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                returnValue = ComputeInternal(buffer, 0, readSize, returnValue);
            } while (readSize > 0);

            return MakeResult(returnValue);
        }

        private uint ComputeInternal(Span<byte> toHash, int offset, int length, uint initialValue)
        {
            var result = initialValue;
            for (var i = offset; i < offset + length; i++)
                result = result * _magic + toHash[i];

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
