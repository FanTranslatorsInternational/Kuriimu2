using Komponent.IO.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Komponent.IO
{
    public static class Tools
    {
        // Endian Swapping Support
        //public static void AdjustByteOrder(Type type, byte[] buffer, ByteOrder byteOrder, int startOffset = 0)
        //{
        //    if (BitConverter.IsLittleEndian == (byteOrder == ByteOrder.LittleEndian)) return;

        //    if (type.IsPrimitive)
        //    {
        //        if (type == typeof(short) || type == typeof(ushort) ||
        //            type == typeof(int) || type == typeof(uint) ||
        //            type == typeof(long) || type == typeof(ulong))
        //        {
        //            Array.Reverse(buffer);
        //            return;
        //        }
        //    }

        //    foreach (var field in type.GetFields())
        //    {
        //        var fieldType = field.FieldType;

        //        // Ignore static fields
        //        if (field.IsStatic) continue;

        //        // Ignore the ByteOrder enum type or else we break byte order detection
        //        if (fieldType.BaseType == typeof(Enum) && fieldType != typeof(ByteOrder))
        //            fieldType = fieldType.GetFields()[0].FieldType;

        //        // Swap bytes only for the following types
        //        // TODO: Add missing types to this list
        //        if (fieldType == typeof(short) || fieldType == typeof(ushort) ||
        //            fieldType == typeof(int) || fieldType == typeof(uint) ||
        //            fieldType == typeof(long) || fieldType == typeof(ulong))
        //        {
        //            var offset = Marshal.OffsetOf(type, field.Name).ToInt32();

        //            // Enums
        //            if (fieldType.IsEnum)
        //                fieldType = Enum.GetUnderlyingType(fieldType);

        //            // Check for sub-fields to recurse if necessary
        //            var subFields = fieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();
        //            var effectiveOffset = startOffset + offset;

        //            if (subFields.Length == 0)
        //                Array.Reverse(buffer, effectiveOffset, Marshal.SizeOf(fieldType));
        //            else
        //                AdjustByteOrder(fieldType, buffer, byteOrder, effectiveOffset);
        //        }
        //    }
        //}

        public static int MeasureType(Type type)
        {
            return MeasureType(type, null, null);
        }

        public static int MeasureTypeUntil(Type type, string limit)
        {
            return MeasureType(type, null, limit);
        }

        private static int MeasureType(Type type, MemberInfo field, string limit)
        {
            var typeAttributes = new MemberAttributeInfo(type);
            MemberAttributeInfo fieldAttributes = null;
            if (field != null) fieldAttributes = new MemberAttributeInfo(field);

            if (type.IsPrimitive)
            {
                return MeasurePrimitive(type);
            }
            else if (type == typeof(decimal))
            {
                return 16;
            }
            else if (type == typeof(string))
            {
                return MeasureString(fieldAttributes);
            }
            else if (IsList(type))
            {
                return MeasureList(type, fieldAttributes);
            }
            else if (type.IsClass || IsStruct(type))
            {
                return MeasureObject(type, limit);
            }
            else if (type.IsEnum)
            {
                return MeasureType(type.GetEnumUnderlyingType());
            }
            else
                throw new UnsupportedTypeException(type);
        }

        private static bool IsList(Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        private static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsEnum;
        }

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

        private static int MeasureString(MemberAttributeInfo attributes)
        {
            if (attributes?.VariableLengthAttribute != null)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
            if (attributes?.FixedLengthAttribute == null)
                throw new InvalidOperationException("Strings without set length are not supported for static measurement");

            var strEnc = attributes?.FixedLengthAttribute.StringEncoding;
            var charSize = 0;
            switch (strEnc)
            {
                case StringEncoding.ASCII: charSize = 1; break;
                case StringEncoding.UTF32: charSize = 4; break;
                default:
                    throw new InvalidOperationException($"Variable width encodings are not supported for static measurement");
            }

            var length = attributes?.FixedLengthAttribute.Length ?? 0;
            return length * charSize;
        }

        private static int MeasureList(Type type, MemberAttributeInfo attributes)
        {
            if (attributes?.VariableLengthAttribute != null)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
            if (attributes?.FixedLengthAttribute == null)
                throw new InvalidOperationException("Strings without set length are not supported for static measurement");

            Type ElementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];

            return attributes?.FixedLengthAttribute.Length ?? 0 * MeasureType(ElementType);
        }

        private static int MeasureObject(Type type, string limit)
        {
            var objectName = limit != null ? Regex.Match(limit, "^[^.]*").Value : null;
            var newLimit = limit?.Contains(".") ?? false ? Regex.Replace(limit, "^[^.]*\\.", "") : null;

            int totalLength = 0;
            foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
            {
                if (field.Name == objectName && string.IsNullOrEmpty(newLimit))
                    break;
                totalLength += MeasureType(field.FieldType, field.CustomAttributes.Any() ? field : null, newLimit);
                if (field.Name == objectName)
                    break;
            }

            return totalLength;
        }
    }
}
