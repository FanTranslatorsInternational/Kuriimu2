using System.Buffers.Binary;

namespace Kryptography.Checksum.Crc
{
    public class Crc16 : Checksum<ushort>
    {
        // https://crccalc.com
        public static Crc16 X25 => new Crc16(0x1021, 0xFFFF, 0xFFFF, true, true);
        public static Crc16 ModBus => new Crc16(0x8005, 0xFFFF, 0x0000, true, true);

        private readonly ushort _polynomial;
        private readonly ushort _initial;
        private readonly ushort _xorOut;
        private readonly bool _reflectIn;
        private readonly bool _reflectOut;

        private Crc16(ushort polynomial, ushort initial, ushort xorOut, bool reflectIn, bool reflectOut)
        {
            _polynomial = polynomial;
            _initial = initial;
            _xorOut = xorOut;
            _reflectIn = reflectIn;
            _reflectOut = reflectOut;
        }

        protected override ushort CreateInitialValue()
        {
            return _initial;
        }

        protected override void FinalizeResult(ref ushort result)
        {
            result ^= _xorOut;
        }

        public override void ComputeBlock(Span<byte> input, ref ushort result)
        {
            foreach (byte value in input)
            {
                byte curByte = _reflectIn ? ReflectByte(value) : value;
                result ^= (ushort)(curByte << 8);

                for (var i = 0; i < 8; i++)
                {
                    if ((result & 0x8000) != 0)
                        result = (ushort)((result << 1) ^ _polynomial);
                    else
                        result <<= 1;
                }
            }

            if (_reflectOut)
                result = ReflectUShort(result);
        }

        protected override byte[] ConvertResult(ushort result)
        {
            var buffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, result);

            return buffer;
        }

        private static byte ReflectByte(byte b)
        {
            byte result = 0;
            for (var i = 0; i < 8; i++)
            {
                if ((b & (1 << i)) != 0)
                    result |= (byte)(1 << (7 - i));
            }
            return result;
        }

        private static ushort ReflectUShort(ushort value)
        {
            ushort result = 0;
            for (var i = 0; i < 16; i++)
            {
                if ((value & (1 << i)) != 0)
                    result |= (ushort)(1 << (15 - i));
            }
            return result;
        }
    }
}
