using System;

namespace Komponent.IO
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
    public class BitFieldInfoAttribute : Attribute
    {
        public int BlockSize = 32;
        public BitOrder BitOrder = BitOrder.Inherit;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class EndiannessAttribute : Attribute
    {
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BitFieldAttribute : Attribute
    {
        public int BitLength { get; }

        public BitFieldAttribute(int bitLength)
        {
            BitLength = bitLength;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class FieldLengthAttribute : Attribute
    {
        public int Length { get; }

        public FieldLengthAttribute(int length)
        {
            Length = length;
        }
    }
}
