using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Models.IO;

namespace Komponent.IO.BinarySupport
{
    class TypeReader
    {
        public object ReadType(BinaryReaderX br, Type readType)
        {
            var storage = new ValueStorage();
            return ReadTypeInternal(br, readType, storage);
        }

        private object ReadTypeInternal(BinaryReaderX br, Type readType, ValueStorage storage, FieldInfo fieldInfo = null, bool isTypeChose = false)
        {
            var typeAttributes = new MemberAttributeInfo(readType);
            var fieldAttributes = fieldInfo == null ? null : new MemberAttributeInfo(fieldInfo);

            var bkByteOrder = br.ByteOrder;
            var bkBitOrder = br.BitOrder;
            var bkBlockSize = br.BlockSize;

            br.ByteOrder = fieldAttributes?.EndiannessAttribute?.ByteOrder ??
                           typeAttributes.EndiannessAttribute?.ByteOrder ??
                           br.ByteOrder;

            object returnValue;
            if (IsTypeChoice(fieldAttributes) && !isTypeChose)
            {
                var chosenType = ChooseType(readType, fieldAttributes, storage);
                returnValue = ReadTypeInternal(br, chosenType, storage, fieldInfo, true);
            }
            else if (readType.IsPrimitive)
            {
                returnValue = ReadTypePrimitive(br, readType);
            }
            else if (readType == typeof(string))
            {
                returnValue = ReadTypeString(br, fieldAttributes, storage);
            }
            else if (readType == typeof(decimal))
            {
                returnValue = br.ReadDecimal();
            }
            else if (Tools.IsList(readType))
            {
                returnValue = ReadTypeList(br, readType, fieldAttributes, storage);
            }
            else if (readType.IsClass || Tools.IsStruct(readType))
            {
                returnValue = ReadTypeClass(br, readType, storage.CreateScope(fieldInfo?.Name));
            }
            else if (readType.IsEnum)
            {
                returnValue = ReadTypeInternal(br, readType.GetEnumUnderlyingType(), storage);
            }
            else throw new UnsupportedTypeException(readType);

            br.ByteOrder = bkByteOrder;
            br.BitOrder = bkBitOrder;
            br.BlockSize = bkBlockSize;

            return returnValue;
        }

        private object ReadTypePrimitive(BinaryReaderX br, Type readType)
        {
            switch (Type.GetTypeCode(readType))
            {
                case TypeCode.Boolean: return br.ReadBoolean();
                case TypeCode.Byte: return br.ReadByte();
                case TypeCode.SByte: return br.ReadSByte();
                case TypeCode.Int16: return br.ReadInt16();
                case TypeCode.UInt16: return br.ReadUInt16();
                case TypeCode.Char: return br.ReadChar();
                case TypeCode.Int32: return br.ReadInt32();
                case TypeCode.UInt32: return br.ReadUInt32();
                case TypeCode.Int64: return br.ReadInt64();
                case TypeCode.UInt64: return br.ReadUInt64();
                case TypeCode.Single: return br.ReadSingle();
                case TypeCode.Double: return br.ReadDouble();
                default: throw new NotSupportedException($"Unsupported Primitive {readType.FullName}.");
            }
        }

        private object ReadTypeString(BinaryReaderX br, MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var attributeValues = GetLengthAttributeValues(fieldAttributes, storage);
            if (attributeValues.HasValue)
            {
                var (length, encoding) = attributeValues.Value;
                return br.ReadString(length, encoding);
            }

            return null;
        }

        private object ReadTypeList(BinaryReaderX br, Type readType, MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var attributeValues = GetLengthAttributeValues(fieldAttributes, storage);
            if (!attributeValues.HasValue)
                return null;

            var (length, _) = attributeValues.Value;

            IList list;
            Type elementType;
            if (readType.IsArray)
            {
                elementType = readType.GetElementType();
                list = Array.CreateInstance(elementType, length);
            }
            else
            {
                elementType = readType.GetGenericArguments()[0];
                list = (IList)Activator.CreateInstance(readType);
            }

            for (var i = 0; i < length; i++)
            {
                var elementValue = ReadTypeInternal(br, elementType, storage.CreateScope($"[{i}]"));
                if (list.IsFixedSize)
                    list[i] = elementValue;
                else
                    list.Add(elementValue);
            }

            return list;
        }

        private object ReadTypeClass(BinaryReaderX br, Type readType, ValueStorage storage)
        {
            var typeAttributes = new MemberAttributeInfo(readType);

            var bitFieldInfoAttribute = typeAttributes.BitFieldInfoAttribute;
            var alignmentAttribute = typeAttributes.AlignmentAttribute;

            if (bitFieldInfoAttribute != null)
                br.ResetBitBuffer();

            br.BitOrder = (bitFieldInfoAttribute?.BitOrder != BitOrder.Default ? bitFieldInfoAttribute?.BitOrder : br.BitOrder) ?? br.BitOrder;
            br.BlockSize = bitFieldInfoAttribute?.BlockSize ?? br.BlockSize;
            if (br.BlockSize != 8 && br.BlockSize != 4 && br.BlockSize != 2 && br.BlockSize != 1)
                throw new InvalidBitFieldInfoException(br.BlockSize);

            var item = Activator.CreateInstance(readType);

            var fields = readType.GetFields().OrderBy(fi => fi.MetadataToken);
            foreach (var field in fields)
            {
                var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();

                object fieldValue;
                if (bitInfo != null)
                    fieldValue = Convert.ChangeType(br.ReadBits(bitInfo.BitLength), field.FieldType);
                else
                    fieldValue = ReadTypeInternal(br, field.FieldType, storage, field);

                storage.Add(field.Name, fieldValue);
                field.SetValue(item, fieldValue);
            }

            if (alignmentAttribute != null)
                br.SeekAlignment(alignmentAttribute.Alignment);

            return item;
        }

        private bool IsTypeChoice(MemberAttributeInfo fieldAttributes)
        {
            return fieldAttributes?.TypeChoiceAttributes?.Any() ?? false;
        }

        private Type ChooseType(Type readType, MemberAttributeInfo fieldAttributes, ValueStorage storage)
        {
            var typeChoices = fieldAttributes.TypeChoiceAttributes.ToArray();

            if (readType != typeof(object) && typeChoices.Any(x => !readType.IsAssignableFrom(x.InjectionType)))
                throw new InvalidOperationException($"Not all type choices are injectable to '{readType.Name}'.");

            Type chosenType = null;
            foreach (var typeChoice in typeChoices)
            {
                if (!storage.Exists(typeChoice.FieldName))
                    throw new InvalidOperationException($"Field '{typeChoice.FieldName}' could not be found.");

                var value = storage.Get(typeChoice.FieldName);
                switch (typeChoice.Comparer)
                {
                    case TypeChoiceComparer.Equal:
                        if (Convert.ToUInt64(value) == Convert.ToUInt64(typeChoice.Value))
                            chosenType = typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Greater:
                        if (Convert.ToUInt64(value) > Convert.ToUInt64(typeChoice.Value))
                            chosenType = typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Smaller:
                        if (Convert.ToUInt64(value) < Convert.ToUInt64(typeChoice.Value))
                            chosenType = typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.GEqual:
                        if (Convert.ToUInt64(value) >= Convert.ToUInt64(typeChoice.Value))
                            chosenType = typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.SEqual:
                        if (Convert.ToUInt64(value) <= Convert.ToUInt64(typeChoice.Value))
                            chosenType = typeChoice.InjectionType;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown comparer {typeChoice.Comparer}.");
                }

                if (chosenType != null)
                    break;
            }

            if (chosenType == null)
                throw new InvalidOperationException($"No choice matched the criteria for injection");

            return chosenType;
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
    }
}
