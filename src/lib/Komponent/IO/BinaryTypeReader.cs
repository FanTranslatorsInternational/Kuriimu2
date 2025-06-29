using System.Collections;
using System.Reflection;
using Komponent.Contract.Enums;
using Komponent.DataClasses;

namespace Komponent.IO
{
    public static class BinaryTypeReader
    {
        private static readonly MemberInfoProvider MemberInfoProvider = new();

        public static T? Read<T>(BinaryReaderX reader)
        {
            return (T?)Read(reader, typeof(T));
        }

        public static object? Read(BinaryReaderX reader, Type type)
        {
            var storage = new ValueStorage();
            return ReadInternal(reader, type, storage);
        }

        public static IList<T?> ReadMany<T>(BinaryReaderX reader, int length)
        {
            var result = new T?[length];
            for (var i = 0; i < length; i++)
                result[i] = Read<T>(reader);

            return result;
        }

        public static IList<object?> ReadMany(BinaryReaderX reader, Type type, int length)
        {
            var result = new object?[length];
            for (var i = 0; i < length; i++)
                result[i] = Read(reader, type);

            return result;
        }

        private static object? ReadInternal(BinaryReaderX reader, Type type, ValueStorage storage, FieldInfo? fieldInfo = null, bool isTypeChosen = false)
        {
            ByteOrder bkByteOrder = reader.ByteOrder;
            BitOrder bkBitOrder = reader.BitOrder;
            int bkBlockSize = reader.BlockSize;

            reader.ByteOrder = MemberInfoProvider.GetByteOrder(fieldInfo, MemberInfoProvider.GetByteOrder(type, reader.ByteOrder));

            object? returnValue = null;
            if (IsTypeChoice(fieldInfo) && !isTypeChosen)
            {
                IList<TypeChoice> typeChoices = MemberInfoProvider.GetTypeChoices(fieldInfo);
                Type chosenType = ChooseType(type, typeChoices, storage);

                returnValue = ReadInternal(reader, chosenType, storage, fieldInfo, true);
            }
            else if (type.IsPrimitive)
            {
                returnValue = ReadTypePrimitive(reader, type);
            }
            else if (type == typeof(string))
            {
                LengthInfo? lengthInfo = MemberInfoProvider.GetLengthInfo(fieldInfo, storage);
                returnValue = ReadTypeString(reader, lengthInfo);
            }
            else if (type == typeof(decimal))
            {
                returnValue = reader.ReadDecimal();
            }
            else if (IsList(type))
            {
                LengthInfo? lengthInfo = MemberInfoProvider.GetLengthInfo(fieldInfo, storage);
                if (lengthInfo != null)
                    returnValue = ReadList(reader, type, lengthInfo, storage, fieldInfo?.Name);
            }
            else if (type.IsClass || IsStruct(type))
            {
                returnValue = ReadComplex(reader, type, storage.CreateScope(fieldInfo?.Name));
            }
            else if (type.IsEnum)
            {
                returnValue = ReadInternal(reader, type.GetEnumUnderlyingType(), storage);
            }
            else throw new InvalidOperationException($"Type {type} is not supported.");

            reader.ByteOrder = bkByteOrder;
            reader.BitOrder = bkBitOrder;
            reader.BlockSize = bkBlockSize;

            return returnValue;
        }

        private static object ReadTypePrimitive(BinaryReaderX reader, Type readType)
        {
            switch (Type.GetTypeCode(readType))
            {
                case TypeCode.Boolean: return reader.ReadBoolean();
                case TypeCode.Byte: return reader.ReadByte();
                case TypeCode.SByte: return reader.ReadSByte();
                case TypeCode.Int16: return reader.ReadInt16();
                case TypeCode.UInt16: return reader.ReadUInt16();
                case TypeCode.Char: return reader.ReadChar();
                case TypeCode.Int32: return reader.ReadInt32();
                case TypeCode.UInt32: return reader.ReadUInt32();
                case TypeCode.Int64: return reader.ReadInt64();
                case TypeCode.UInt64: return reader.ReadUInt64();
                case TypeCode.Single: return reader.ReadSingle();
                case TypeCode.Double: return reader.ReadDouble();
                default: throw new NotSupportedException($"Unsupported primitive {readType}.");
            }
        }

        private static object ReadTypeString(BinaryReaderX reader, LengthInfo? lengthInfo)
        {
            // If no length attributes are given, assume string with 7bit-encoded int length prefixing the string
            if (lengthInfo == null)
                return reader.ReadString();

            return reader.ReadString(lengthInfo.Length, lengthInfo.Encoding);
        }

        private static object ReadList(BinaryReaderX reader, Type type, LengthInfo lengthInfo, ValueStorage storage, string? listFieldName)
        {
            IList list;
            Type elementType;

            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                list = Array.CreateInstance(elementType, lengthInfo.Length);
            }
            else
            {
                elementType = type.GetGenericArguments()[0];
                list = (IList)Activator.CreateInstance(type)!;
            }

            for (var i = 0; i < lengthInfo.Length; i++)
            {
                object? elementValue = ReadInternal(reader, elementType, storage.CreateScope($"{listFieldName}[{i}]"));
                if (list.IsFixedSize)
                    list[i] = elementValue;
                else
                    list.Add(elementValue);
            }

            return list;
        }

        private static object ReadComplex(BinaryReaderX reader, Type type, ValueStorage storage)
        {
            BitFieldInfo? bitField = MemberInfoProvider.GetBitFieldInfo(type);
            int? alignment = MemberInfoProvider.GetAlignment(type);

            if (bitField != null)
                reader.ResetBitBuffer();

            reader.BitOrder = bitField?.BitOrder ?? reader.BitOrder;
            reader.BlockSize = bitField?.BlockSize ?? reader.BlockSize;

            object item = Activator.CreateInstance(type)!;
            
            var fields = type.GetFields().OrderBy(fi => fi.MetadataToken);
            foreach (FieldInfo? field in fields)
            {
                // If field condition is false, read no value and leave field to default
                ConditionInfo? condition = MemberInfoProvider.GetConditionInfo(field);
                if (!ResolveCondition(condition, storage))
                    continue;

                int? bitLength = MemberInfoProvider.GetBitLength(field);

                object? fieldValue = bitLength.HasValue 
                    ? reader.ReadBits(bitLength.Value) 
                    : ReadInternal(reader, field.FieldType, storage, field);

                storage.Set(field.Name, fieldValue);
                field.SetValue(item, Convert.ChangeType(fieldValue, field.FieldType));
            }

            if (alignment != null)
                reader.SeekAlignment(alignment.Value);

            return item;
        }

        private static bool IsTypeChoice(MemberInfo? field)
        {
            return MemberInfoProvider.GetTypeChoices(field).Count > 0;
        }

        private static Type ChooseType(Type readType, IList<TypeChoice> typeChoices, ValueStorage storage)
        {
            if (readType != typeof(object) && typeChoices.Any(x => !readType.IsAssignableFrom(x.InjectionType)))
                throw new InvalidOperationException($"Not all type choices are injectable to '{readType.Name}'.");

            foreach (var typeChoice in typeChoices)
            {
                if (!storage.Exists(typeChoice.FieldName))
                    throw new InvalidOperationException($"Field '{typeChoice.FieldName}' could not be found.");

                var value = storage.Get(typeChoice.FieldName);
                switch (typeChoice.Comparer)
                {
                    case TypeChoiceComparer.Equal:
                        if (Convert.ToUInt64(value) == Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Greater:
                        if (Convert.ToUInt64(value) > Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Smaller:
                        if (Convert.ToUInt64(value) < Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.GEqual:
                        if (Convert.ToUInt64(value) >= Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.SEqual:
                        if (Convert.ToUInt64(value) <= Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown comparer {typeChoice.Comparer}.");
                }
            }

            throw new InvalidOperationException("No choice matched the criteria for injection");
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
