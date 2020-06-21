using System;
using System.IO;

#if NET_CORE_31
using System.Buffers.Binary;
#endif

// https://stackoverflow.com/questions/10564491/function-to-calculate-a-crc16-checksum
namespace Kryptography.Hash.Crc
{
    public class Crc16 : IHash
    {
        public static Crc16 Create(Crc16Formula formula)
        {
            switch (formula)
            {
                case Crc16Formula.X25:
                    return new Crc16(0x8408, 0xFFFF, 0xFFFF);

                default:
                    throw new NotSupportedException();
            }
        }

        private readonly int _polynomial;
        private readonly int _initial;
        private readonly int _xorOut;

        private Crc16(int polynomial, int initial, int xorOut)
        {
            _polynomial = polynomial;
            _initial = initial;
            _xorOut = xorOut;
        }

        public byte[] Compute(Stream input)
        {
            var returnCrc = _initial;

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                returnCrc = ComputeInternal(buffer, 0, readSize, returnCrc);
            } while (readSize > 0);

            return MakeResult(returnCrc ^ _xorOut);
        }

        public byte[] Compute(byte[] input)
        {
            var result = ComputeInternal(input, 0, input.Length, _initial);

            return MakeResult(result ^ _xorOut);
        }

        private int ComputeInternal(byte[] toHash, int offset, int length, int initialCrc)
        {
            var returnCrc = initialCrc;

            while (length-- > 0)
            {
                returnCrc ^= toHash[offset++];
                for (var k = 0; k < 8; k++)
                    returnCrc = (returnCrc & 1) > 0 ?
                        (returnCrc >> 1) ^ _polynomial :
                        returnCrc >> 1;
            }

            return returnCrc;
        }

        private byte[] MakeResult(int result)
        {
            var returnBuffer = new byte[2];

#if NET_CORE_31
            BinaryPrimitives.WriteUInt16BigEndian(returnBuffer, (ushort)result);
#else
            returnBuffer[0] = (byte)(result >> 8);
            returnBuffer[1] = (byte)result;
#endif

            return returnBuffer;
        }
    }
}
