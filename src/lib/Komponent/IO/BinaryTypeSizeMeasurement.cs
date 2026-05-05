using System.Collections;
using System.Reflection;
using Komponent.Contract.Exceptions;
using Komponent.DataClasses;

namespace Komponent.IO
{
    internal class BinaryTypeSizeMeasurement
    {
        private static readonly MemberInfoProvider MemberInfoProvider = new();

        public int Measure<T>()
        {
            return Measure(typeof(T));
        }

        public int Measure(Type type)
        {
            return MeasureType(type, null);
        }

        private int MeasureType(Type type, MemberInfo? field)
        {
            if (MemberInfoProvider.GetTypeChoices(field).Any())
                throw new InvalidOperationException("Type choice attributes are not supported for static measurement.");

            if (type.IsPrimitive)
                return MeasurePrimitive(type);

            if (type == typeof(decimal))
                return 16;

            if (type == typeof(string))
                return MeasureString(field, MemberInfoProvider.GetLengthInfoSource(field));

            if (IsList(type))
                return MeasureList(field, type, MemberInfoProvider.GetLengthInfoSource(field));

            if (type.IsClass || IsStruct(type))
                return MeasureComplex(type);

            if (type.IsEnum)
                return Measure(type.GetEnumUnderlyingType());

            throw new UnsupportedTypeException(type);
        }

        private static int MeasurePrimitive(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => 1,
                TypeCode.Byte => 1,
                TypeCode.SByte => 1,
                TypeCode.Int16 => 2,
                TypeCode.UInt16 => 2,
                TypeCode.Char => 2,
                TypeCode.Int32 => 4,
                TypeCode.UInt32 => 4,
                TypeCode.Int64 => 8,
                TypeCode.UInt64 => 8,
                TypeCode.Single => 4,
                TypeCode.Double => 8,
                _ => throw new NotSupportedException($"Unsupported primitive type {type.Name}.")
            };
        }

        private static int MeasureString(MemberInfo? field, LengthInfoSource? source)
        {
            if (source == LengthInfoSource.Variable)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement.");
            if (source == LengthInfoSource.Calculation)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement.");
            if (source != LengthInfoSource.Fixed)
                throw new InvalidOperationException("Strings without set length are not supported for static measurement.");

            return MemberInfoProvider.GetLengthInfo(field, null)!.Length;
        }

        private int MeasureList(MemberInfo? field, Type fieldType, LengthInfoSource? source)
        {
            if (source == LengthInfoSource.Variable)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement.");
            if (source == LengthInfoSource.Calculation)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement.");
            if (source != LengthInfoSource.Fixed)
                throw new InvalidOperationException("Lists without set length are not supported for static measurement.");

            Type? elementType = fieldType.IsArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0];

            return MemberInfoProvider.GetLengthInfo(field, null)!.Length * Measure(elementType!);
        }

        private int MeasureComplex(Type type)
        {
            var totalLength = 0;
            foreach (FieldInfo? field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                totalLength += MeasureType(field.FieldType, field.CustomAttributes.Any() ? field : null);
            
            return totalLength;
        }

        private static bool IsList(Type type)
        {
            return type.IsAssignableTo(typeof(IList));
        }

        private static bool IsStruct(Type type)
        {
            return type is { IsValueType: true, IsEnum: false };
        }
    }
}
