﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Komponent.Exceptions;
using Komponent.IO.Attributes;
using Komponent.IO.BinarySupport;

namespace Komponent.IO
{
    public static class Tools
    {
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
            var fieldAttributes = field != null ? new MemberAttributeInfo(field) : null;

            if (fieldAttributes?.TypeChoiceAttributes.Any() ?? false)
                throw new InvalidOperationException("Type choice attributes are not supported for static measurement");

            if (type.IsPrimitive)
                return MeasurePrimitive(type);

            if (type == typeof(decimal))
                return 16;

            if (type == typeof(string))
                return MeasureString(fieldAttributes);

            if (IsList(type))
                return MeasureList(type, fieldAttributes);

            if (type.IsClass || IsStruct(type))
                return MeasureObject(type, limit);

            if (type.IsEnum)
                return MeasureType(type.GetEnumUnderlyingType());

            throw new UnsupportedTypeException(type);
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
            if (attributes?.CalculatedLengthAttribute != null)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement");
            if (attributes?.FixedLengthAttribute == null)
                throw new InvalidOperationException("Strings without set length are not supported for static measurement");

            return attributes.FixedLengthAttribute.Length;
        }

        private static int MeasureList(Type type, MemberAttributeInfo attributes)
        {
            if (attributes?.VariableLengthAttribute != null)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement");
            if (attributes?.CalculatedLengthAttribute != null)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement");
            if (attributes?.FixedLengthAttribute == null)
                throw new InvalidOperationException("Lists without set length are not supported for static measurement");

            var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];

            return attributes.FixedLengthAttribute.Length * MeasureType(elementType);
        }

        private static int MeasureObject(Type type, string limit)
        {
            var objectName = limit != null ? Regex.Match(limit, "^[^.]*").Value : null;
            var newLimit = limit?.Contains(".") ?? false ? Regex.Replace(limit, "^[^.]*\\.", "") : null;

            var totalLength = 0;
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

        internal static bool IsList(Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        internal static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsEnum;
        }

        internal static Encoding RetrieveEncoding(StringEncoding strEnc)
        {
            switch (strEnc)
            {
                default: return Encoding.ASCII;
                case StringEncoding.SJIS: return Encoding.GetEncoding("SJIS");
                case StringEncoding.Unicode: return Encoding.Unicode;
                case StringEncoding.UTF16: return Encoding.Unicode;
                case StringEncoding.UTF32: return Encoding.UTF32;
#pragma warning disable SYSLIB0001 // Typ oder Element ist veraltet
                case StringEncoding.UTF7: return Encoding.UTF7;
#pragma warning restore SYSLIB0001 // Typ oder Element ist veraltet
                case StringEncoding.UTF8: return Encoding.UTF8;
            }
        }
    }
}
