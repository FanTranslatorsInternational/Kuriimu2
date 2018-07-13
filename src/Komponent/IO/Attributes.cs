using System;
using System.Text;

namespace Komponent.IO
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
    public class BitFieldInfoAttribute : Attribute
    {
        public int BlockSize = 32;
        public BitOrder BitOrder = BitOrder.Inherit;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
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
    public class FixedLengthAttribute : Attribute
    {
        public int Length { get; }
        public StringEncoding StringEncoding = StringEncoding.ASCII;

        public FixedLengthAttribute(int length)
        {
            Length = length;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class VariableLengthAttribute : Attribute
    {
        public string FieldName { get; }
        public StringEncoding StringEncoding = StringEncoding.ASCII;

        public VariableLengthAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    public enum StringEncoding : byte
    {
        ASCII,
        UTF7,
        UTF8,
        UTF16,
        Unicode,
        UTF32,
        SJIS
    }
}
