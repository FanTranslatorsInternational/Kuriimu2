using System;
using System.Buffers.Binary;

// https://stackoverflow.com/questions/10564491/function-to-calculate-a-crc16-checksum
// Online tool to check implementation: https://crccalc.com
//      This tool seems to utilize a different approach to applying the polynomial.
//      There are 2 ways a polynomial can be read and applied, read from LSB to MSB, or vice versa
//      Therefore, depending on the implementation, e.g X25 can have a valid polynomial of 0x8404 or 0x1021
//      If the polynomials of any CRC16 implementation from the link above is used, its bits have to be reversed, to work properly with this algorithm.
namespace Kryptography.Hash.Crc
{
    public class Crc16 : BaseHash<ushort>
    {
        public static Crc16 X25 => new Crc16(0x8408, 0xFFFF, 0xFFFF);

        public static Crc16 ModBus => new Crc16(0xA001, 0xFFFF, 0x0000);

        private readonly ushort _polynomial;
        private readonly ushort _initial;
        private readonly ushort _xorOut;

        private Crc16(ushort polynomial, ushort initial, ushort xorOut)
        {
            _polynomial = polynomial;
            _initial = initial;
            _xorOut = xorOut;
        }

        protected override ushort CreateInitialValue()
        {
            return _initial;
        }

        protected override void FinalizeResult(ref ushort result)
        {
            result ^= _xorOut;
        }

        protected override void ComputeInternal(Span<byte> input, ref ushort result)
        {
            foreach (var value in input)
            {
                result ^= value;
                for (var k = 0; k < 8; k++)
                    result = (result & 1) > 0 ?
                        (ushort)((result >> 1) ^ _polynomial) :
                        (ushort)(result >> 1);
            }
        }

        protected override byte[] ConvertResult(ushort result)
        {
            var buffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, result);

            return buffer;
        }
    }
}
