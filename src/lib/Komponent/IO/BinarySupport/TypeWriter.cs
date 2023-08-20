using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Komponent.Exceptions;
using Komponent.IO.Attributes;
using Kontract.Models.IO;

namespace Komponent.IO.BinarySupport
{
    class TypeWriter
    {
        public void WriteType(BinaryWriterX bw, object writeValue)
        {
            var storage = new ValueStorage();
            WriteTypeInternal(bw, writeValue, storage);
        }

        private void WriteTypeInternal(BinaryWriterX bw, object writeValue, ValueStorage storage, FieldInfo fieldInfo = null, bool isTypeChose = false)
        {
            var writeType = writeValue.GetType();
            var typeAttributes = new MemberAttributeInfo(writeType);
            var fieldAttributes = fieldInfo == null ? null : new MemberAttributeInfo(fieldInfo);

            var bkByteOrder = bw.ByteOrder;
            var bkBitOrder = bw.BitOrder;
            var bkBlockSize = bw.BlockSize;

            bw.ByteOrder = fieldAttributes?.EndiannessAttribute?.ByteOrder ??
                           typeAttributes.EndiannessAttribute?.ByteOrder ??
                           bw.ByteOrder;

            if (writeType.IsPrimitive)
            {
                WriteTypePrimitive(bw, writeValue, writeType);
            }
            else if (writeType == typeof(string))
            {
                WriteTypeString(bw, (string)writeValue, fieldAttributes, storage);
            }
            else if (writeType == typeof(decimal))
            {
                bw.Write((decimal)writeValue);
            }
            else if (Tools.IsList(writeType))
            {
                WriteTypeList(bw, (IList)writeValue, fieldAttributes, storage);
            }
            else if (writeType.IsClass || Tools.IsStruct(writeType))
            {
                WriteTypeClass(bw, writeValue, writeType, storage.CreateScope(fieldInfo?.Name));
            }
            else if (writeType.IsEnum)
            {
                var underlyingType = (writeType as TypeInfo)?.DeclaredFields.ToList()[0];
                WriteTypeInternal(bw, underlyingType?.GetValue(writeValue), storage);
            }
            else throw new UnsupportedTypeException(writeType);

            bw.ByteOrder = bkByteOrder;
            bw.BitOrder = bkBitOrder;
            bw.BlockSize = bkBlockSize;
        }

        private void WriteTypePrimitive(BinaryWriterX bw, object writeValue, Type writeType)
        {
            switch (Type.GetTypeCode(writeType))
            {
                case TypeCode.Boolean: bw.Write((bool)writeValue); break;
                case TypeCode.Byte: bw.Write((byte)writeValue); break;
                case TypeCode.SByte: bw.Write((sbyte)writeValue); break;
                case TypeCode.Int16: bw.Write((short)writeValue); break;
                case TypeCode.UInt16: bw.Write((ushort)writeValue); break;
                case TypeCode.Char: bw.Write((char)writeValue); break;
                case TypeCode.Int32: bw.Write((int)writeValue); break;
                case TypeCode.UInt32: bw.Write((uint)writeValue); break;
                case TypeCode.Int64: bw.Write((long)writeValue); break;
                case TypeCode.UInt64: bw.Write((ulong)writeValue); break;
                case TypeCode.Single: bw.Write((float)writeValue); break;
                case TypeCode.Double: bw.Write((double)writeValue); break;
                default: throw new NotSupportedException($"Unsupported Primitive {writeType.FullName}.");
            }
        }

        private void WriteTypeString(BinaryWriterX bw, string writeValue, MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var attributeValues = GetLengthAttributeValues(fieldAttributes, storage);

            // If no length attributes are given, assume string with 7bit-encoded int length prefixing the string
            if (!attributeValues.HasValue)
            {
                bw.Write(writeValue);
                return;
            }

            var (length, encoding) = attributeValues.Value;
            bw.Write(ConvertStringValue(writeValue, encoding, length));
        }

        private void WriteTypeList(BinaryWriterX bw, IList writeValue, MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var attributeValues = GetLengthAttributeValues(fieldAttributes, storage);
            if (!attributeValues.HasValue)
                return;

            var (length, _) = attributeValues.Value;

            if (writeValue.Count != length)
                throw new FieldLengthMismatchException(writeValue.Count, length);

            var listCounter = 0;
            foreach (var element in writeValue)
                WriteTypeInternal(bw, element, storage.CreateScope($"[{listCounter++}]"));
        }

        private void WriteTypeClass(BinaryWriterX bw, object writeValue, Type writeType, ValueStorage storage)
        {
            var typeAttributes = new MemberAttributeInfo(writeType);

            var bitFieldInfoAttribute = typeAttributes.BitFieldInfoAttribute;
            var alignmentAttribute = typeAttributes.AlignmentAttribute;

            if (bitFieldInfoAttribute != null)
                bw.Flush();

            bw.BitOrder = (bitFieldInfoAttribute?.BitOrder != BitOrder.Default ? bitFieldInfoAttribute?.BitOrder : bw.BitOrder) ?? bw.BitOrder;
            bw.BlockSize = bitFieldInfoAttribute?.BlockSize ?? bw.BlockSize;
            if (bw.BlockSize != 8 && bw.BlockSize != 4 && bw.BlockSize != 2 && bw.BlockSize != 1)
                throw new InvalidBitFieldInfoException(bw.BlockSize);

            var fields = writeType.GetFields().OrderBy(fi => fi.MetadataToken);
            foreach (var field in fields)
            {
                // If field condition is false, write no value and ignore field
                var conditionAttribute = field.GetCustomAttribute<ConditionAttribute>();
                if (!ResolveCondition(conditionAttribute, storage))
                    continue;

                var fieldValue = field.GetValue(writeValue);
                storage.Add(field.Name, fieldValue);

                var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();
                if (bitInfo != null)
                    bw.WriteBits(Convert.ToInt64(fieldValue), bitInfo.BitLength);
                else
                    WriteTypeInternal(bw, fieldValue, storage, field);
            }

            bw.Flush();

            // Apply alignment
            if (alignmentAttribute != null)
                bw.WriteAlignment(alignmentAttribute.Alignment);
        }

        private (int, Encoding)? GetLengthAttributeValues(MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var fixedLengthAttribute = fieldAttributes?.FixedLengthAttribute;
            var variableLengthAttribute = fieldAttributes?.VariableLengthAttribute;
            var calculatedLengthAttribute = fieldAttributes?.CalculatedLengthAttribute;

            Encoding stringEncoding;
            int length;
            if (fixedLengthAttribute != null)
            {
                stringEncoding = Tools.RetrieveEncoding(fixedLengthAttribute.StringEncoding);
                length = fixedLengthAttribute.Length;
            }
            else if (variableLengthAttribute != null)
            {
                stringEncoding = Tools.RetrieveEncoding(variableLengthAttribute.StringEncoding);
                length = Convert.ToInt32(storage.Get(variableLengthAttribute.FieldName)) + variableLengthAttribute.Offset;
            }
            else if (calculatedLengthAttribute != null)
            {
                stringEncoding = Tools.RetrieveEncoding(calculatedLengthAttribute.StringEncoding);
                length = calculatedLengthAttribute.CalculationAction(storage);
            }
            else
            {
                return null;
            }

            return (length, stringEncoding);
        }

        private bool ResolveCondition(ConditionAttribute condition, ValueStorage storage)
        {
            // If no condition is given, resolve it to true so the field is read
            if (condition == null)
                return true;

            var value = storage.Get(condition.FieldName);
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

        private byte[] ConvertStringValue(string value, Encoding encoding, int byteLength)
        {
            var buffer = new byte[byteLength];
            var convertedValue = encoding.GetBytes(value);

            Array.Copy(convertedValue, 0, buffer, 0, Math.Min(byteLength, convertedValue.Length));

            return buffer;
        }
    }
}
