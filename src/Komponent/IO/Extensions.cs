using Komponent.IO.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Komponent.IO
{
    internal class DecimalExtensions
    {
        public static byte[] GetBytes(decimal value)
        {
            var bits = decimal.GetBits(value);
            var bytes = new List<byte>();

            foreach (var i in bits)
                bytes.AddRange(BitConverter.GetBytes(i));

            return bytes.ToArray();
        }

        public static decimal ToDecimal(byte[] value)
        {
            if (value.Length != 16)
                throw new Exception("A decimal must be created from exactly 16 bytes");

            var bits = new int[4];
            for (var i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(value, i);

            return new decimal(bits);
        }
    }

    public class MeasurementMethods
    {
        private static int MeasurePrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return 1;
                case TypeCode.Byte: return 1;
                case TypeCode.SByte: return 1;
                case TypeCode.Int16: return 2;
                case TypeCode.UInt16: return 2;
                case TypeCode.Char: return 2;
                case TypeCode.Int32: return 4;
                case TypeCode.UInt32: return 4;
                case TypeCode.Int64: return 8;
                case TypeCode.UInt64: return 8;
                case TypeCode.Single: return 4;
                case TypeCode.Double: return 8;
                default: throw new NotSupportedException("Unsupported Primitive");
            }
        }

        private static int MeasureStruct(Type type, MemberInfo fieldInfo = null)
        {
            var FixedSize = fieldInfo?.GetCustomAttribute<FixedLengthAttribute>();
            var VarSize = fieldInfo?.GetCustomAttribute<VariableLengthAttribute>();
            //var BitFieldInfo = type.GetCustomAttribute<BitFieldInfoAttribute>();

            if (type.IsPrimitive)
            {
                //Primitive
                return MeasurePrimitive(type);
            }
            else if (Type.GetTypeCode(type) == TypeCode.String)
            {
                // String
                if (VarSize != null)
                    throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
                if (FixedSize == null)
                    throw new InvalidOperationException("Strings without set length are not supported for static measurement");

                var strEnc = FixedSize.StringEncoding;
                var charSize = 0;
                switch (strEnc)
                {
                    case StringEncoding.ASCII: charSize = 1; break;
                    case StringEncoding.UTF32: charSize = 4; break;
                    default:
                        throw new InvalidOperationException($"Variable width encodings are not supported for static measurement");
                }

                var length = FixedSize.Length;
                return length * charSize;
            }
            else if (Type.GetTypeCode(type) == TypeCode.Decimal)
            {
                // Decimal
                return 16;
            }
            else if (type.IsArray)
            {
                // Array
                if (VarSize != null)
                    throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
                if (FixedSize == null)
                    throw new InvalidOperationException("Arrays without set length are not supported for static measurement");

                var length = FixedSize.Length;
                var elementLength = MeasureStruct(type.GetElementType());

                return length * elementLength;
            }
            else if (type.IsGenericType && type.Name.Contains("List"))
            {
                // List
                if (VarSize != null)
                    throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
                if (FixedSize == null)
                    throw new InvalidOperationException("ILists without set length are not supported for static measurement");

                var length = FixedSize.Length;
                var elementLength = MeasureStruct(type.GenericTypeArguments.First());

                return length * elementLength;
            }
            else if (type.IsClass || type.IsValueType && !type.IsEnum)
            {
                // Class, Struct
                int totalLength = 0;
                foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                    totalLength += MeasureStruct(field.FieldType, field.CustomAttributes.Any() ? field : null);

                return totalLength;
            }
            else if (type.IsEnum)
            {
                // Enum
                return MeasureStruct(type.GetEnumUnderlyingType());
            }
            else throw new UnsupportedTypeException(type);
        }

        public static int MeasureStruct<T>() where T : class
            => MeasureStruct(typeof(T));
    }

    public static class IOExtensions
    {
        public static byte[] Hexlify(this string input, int length = -1)
        {
            int NumberChars = input.Length;
            byte[] bytes = new byte[(length < 0) ? NumberChars / 2 : (length + 1) & ~1];
            for (int i = 0; i < ((length < 0) ? NumberChars : length * 2); i += 2)
                bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
            return bytes;
        }

        public static string Stringify(this byte[] input, int length = -1)
        {
            string result = "";
            for (int i = 0; i < (length < 0 ? input.Length : length); i++)
                result += input[i].ToString("X2");
            return result;
        }
    }
}
