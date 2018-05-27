using System;

namespace Komponent.IO
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BitFieldInfo : Attribute
    {
        public int BlockSize = 32;
        public BitOrder BitOrder = BitOrder.Inherit;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class Endianness : Attribute
    {
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BitField : Attribute
    {
        public int BitLength { get; }

        public BitField(int bitLength)
        {
            BitLength = bitLength;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class FieldLength : Attribute
    {
        public int Length { get; }

        public FieldLength(int length)
        {
            Length = length;
        }
    }
}
