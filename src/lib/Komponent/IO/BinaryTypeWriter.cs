using System.Collections;
using System.Reflection;
using Komponent.Contract.Enums;
using Komponent.Contract.Exceptions;
using Komponent.DataClasses;

namespace Komponent.IO
{
    public static class BinaryTypeWriter
    {
        private static readonly MemberInfoProvider MemberInfoProvider = new();

        public static void Write(object value, BinaryWriterX writer)
        {
            var storage = new ValueStorage();
            WriteInternal(value, value.GetType(), writer, storage);
        }

        public static void WriteMany<T>(IEnumerable<T> list, BinaryWriterX writer)
        {
            foreach (T element in list)
                Write(element, writer);
        }

        private static void WriteInternal(object writeValue, Type writeType, BinaryWriterX writer, ValueStorage storage, FieldInfo? fieldInfo = null)
        {
            ByteOrder bkByteOrder = writer.ByteOrder;
            BitOrder bkBitOrder = writer.BitOrder;
            int bkBlockSize = writer.BlockSize;

            writer.ByteOrder = MemberInfoProvider.GetByteOrder(fieldInfo, MemberInfoProvider.GetByteOrder(writeType, writer.ByteOrder));

            if (writeType.IsPrimitive)
            {
                WritePrimitive(writeValue, writeType, writer);
            }
            else if (writeType == typeof(string))
            {
                LengthInfo? lengthInfo = MemberInfoProvider.GetLengthInfo(fieldInfo, storage);
                WriteString((string)writeValue, writer, lengthInfo);
            }
            else if (writeType == typeof(decimal))
            {
                writer.Write((decimal)writeValue);
            }
            else if (IsList(writeType))
            {
                LengthInfo? lengthInfo = MemberInfoProvider.GetLengthInfo(fieldInfo, storage);
                if (lengthInfo != null)
                    WriteList((IList)writeValue, writer, lengthInfo, storage);
            }
            else if (writeType.IsClass || IsStruct(writeType))
            {
                WriteComplex(writeValue, writeType, writer, storage.CreateScope(fieldInfo?.Name));
            }
            else if (writeType.IsEnum)
            {
                FieldInfo? underlyingType = (writeType as TypeInfo)?.DeclaredFields.ToList()[0];
                if (underlyingType != null)
                    WriteInternal(underlyingType.GetValue(writeValue)!, underlyingType.FieldType, writer, storage);
            }
            else throw new UnsupportedTypeException(writeType);

            writer.ByteOrder = bkByteOrder;
            writer.BitOrder = bkBitOrder;
            writer.BlockSize = bkBlockSize;
        }

        private static void WritePrimitive(object writeValue, Type writeType, BinaryWriterX writer)
        {
            switch (Type.GetTypeCode(writeType))
            {
                case TypeCode.Boolean: writer.Write((bool)writeValue); break;
                case TypeCode.Byte: writer.Write((byte)writeValue); break;
                case TypeCode.SByte: writer.Write((sbyte)writeValue); break;
                case TypeCode.Int16: writer.Write((short)writeValue); break;
                case TypeCode.UInt16: writer.Write((ushort)writeValue); break;
                case TypeCode.Char: writer.Write((char)writeValue); break;
                case TypeCode.Int32: writer.Write((int)writeValue); break;
                case TypeCode.UInt32: writer.Write((uint)writeValue); break;
                case TypeCode.Int64: writer.Write((long)writeValue); break;
                case TypeCode.UInt64: writer.Write((ulong)writeValue); break;
                case TypeCode.Single: writer.Write((float)writeValue); break;
                case TypeCode.Double: writer.Write((double)writeValue); break;
                default: throw new NotSupportedException($"Unsupported primitive {writeType.FullName}.");
            }
        }

        private static void WriteString(string writeValue, BinaryWriterX writer, LengthInfo? lengthInfo)
        {
            // If no length attributes are given, assume string with 7bit-encoded int length prefixing the string
            if (lengthInfo == null)
            {
                writer.Write(writeValue);
                return;
            }

            byte[] stringBytes = lengthInfo.Encoding.GetBytes(writeValue);
            stringBytes = ClampBuffer(stringBytes, lengthInfo.Length);

            writer.Write(stringBytes);
        }

        private static void WriteList(IList writeValue, BinaryWriterX writer, LengthInfo lengthInfo, ValueStorage storage)
        {
            if (writeValue.Count != lengthInfo.Length)
                throw new FieldLengthMismatchException(writeValue.Count, lengthInfo.Length);

            var listCounter = 0;
            foreach (object value in writeValue)
                WriteInternal(value, value.GetType(), writer, storage.CreateScope($"[{listCounter++}]"));
        }

        private static void WriteComplex(object writeValue, Type writeType, BinaryWriterX writer, ValueStorage storage)
        {
            BitFieldInfo? bitField = MemberInfoProvider.GetBitFieldInfo(writeType);
            int? alignment = MemberInfoProvider.GetAlignment(writeType);

            if (bitField != null)
                writer.Flush();

            if (bitField != null)
                writer.Flush();

            writer.BitOrder = bitField?.BitOrder ?? writer.BitOrder;
            writer.BlockSize = bitField?.BlockSize ?? writer.BlockSize;

            var fields = writeType.GetFields().OrderBy(fi => fi.MetadataToken);
            foreach (FieldInfo? field in fields)
            {
                // If field condition is false, write no value and ignore field
                ConditionInfo? condition = MemberInfoProvider.GetConditionInfo(field);
                if (!ResolveCondition(condition, storage))
                    continue;

                object? fieldValue = field.GetValue(writeValue);
                storage.Set(field.Name, fieldValue);

                int? bitLength = MemberInfoProvider.GetBitLength(field);
                if (bitLength != null)
                    writer.WriteBits(Convert.ToInt64(fieldValue), bitLength.Value);
                else
                    WriteInternal(fieldValue, field.FieldType, writer, storage, field);
            }

            writer.Flush();

            // Apply alignment
            if (alignment != null)
                writer.WriteAlignment(alignment.Value);
        }

        private static bool ResolveCondition(ConditionInfo? condition, ValueStorage storage)
        {
            // If no condition is given, resolve it to true so the field is read
            if (condition == null)
                return true;

            object? value = storage.Get(condition.FieldName);
            switch (condition.Comparer)
            {
                case ConditionComparer.Equal:
                    return Convert.ToUInt64(value) == condition.Value;

                case ConditionComparer.Greater:
                    return Convert.ToUInt64(value) > condition.Value;

                case ConditionComparer.Smaller:
                    return Convert.ToUInt64(value) < condition.Value;

                case ConditionComparer.GEqual:
                    return Convert.ToUInt64(value) >= condition.Value;

                case ConditionComparer.SEqual:
                    return Convert.ToUInt64(value) <= condition.Value;

                default:
                    throw new InvalidOperationException($"Unknown comparer {condition.Comparer}.");
            }
        }

        private static byte[] ClampBuffer(byte[] input, int length)
        {
            var buffer = new byte[length];

            Array.Copy(input, 0, buffer, 0, Math.Min(length, input.Length));

            return buffer;
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
