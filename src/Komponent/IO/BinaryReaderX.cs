using Komponent.IO.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kontract.Models.IO;

namespace Komponent.IO
{
    public sealed class BinaryReaderX : BinaryReader
    {
        private int _nibble = -1;
        private int _blockSize;
        private int _currentBlockSize;
        private int _bitPosition = 64;
        private long _buffer;

        public ByteOrder ByteOrder { get; set; }
        public BitOrder BitOrder { get; set; }
        public NibbleOrder NibbleOrder { get; set; }
        public bool IsFirstNibble => _nibble == -1;

        private Encoding _encoding = Encoding.UTF8;

        public BitOrder EffectiveBitOrder
        {
            get
            {
                if (ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.LowestAddressFirst || ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.HighestAddressFirst)
                    return BitOrder.LeastSignificantBitFirst;

                if (ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.HighestAddressFirst || ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.LowestAddressFirst)
                    return BitOrder.MostSignificantBitFirst;

                return BitOrder;
            }
        }

        public int BlockSize
        {
            get => _currentBlockSize;
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 8)
                    throw new InvalidOperationException("BlockSize can only be 1, 2, 4, or 8.");

                _blockSize = value;
                _currentBlockSize = value;
            }
        }

        #region Constructors

        public BinaryReaderX(Stream input,
            ByteOrder byteOrder = ByteOrder.LittleEndian,
            NibbleOrder nibbleOrder = NibbleOrder.LowNibbleFirst,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
            int blockSize = 4) : base(input, Encoding.UTF8)
        {
            ByteOrder = byteOrder;
            NibbleOrder = nibbleOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryReaderX(Stream input,
            bool leaveOpen,
            ByteOrder byteOrder = ByteOrder.LittleEndian,
            NibbleOrder nibbleOrder = NibbleOrder.LowNibbleFirst,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
            int blockSize = 4) : base(input, Encoding.UTF8, leaveOpen)
        {
            ByteOrder = byteOrder;
            NibbleOrder = nibbleOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryReaderX(Stream input,
            Encoding encoding,
            ByteOrder byteOrder = ByteOrder.LittleEndian,
            NibbleOrder nibbleOrder = NibbleOrder.LowNibbleFirst,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
            int blockSize = 4) : base(input, encoding)
        {
            ByteOrder = byteOrder;
            NibbleOrder = nibbleOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
            _encoding = encoding;
        }

        public BinaryReaderX(Stream input,
            Encoding encoding,
            bool leaveOpen,
            ByteOrder byteOrder = ByteOrder.LittleEndian,
            NibbleOrder nibbleOrder = NibbleOrder.LowNibbleFirst,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
            int blockSize = 4) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            NibbleOrder = nibbleOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;

            _encoding = encoding;
        }

        #endregion

        #region Default Reads

        public override byte ReadByte()
        {
            Reset();

            return base.ReadByte();
        }

        public override sbyte ReadSByte()
        {
            Reset();

            return base.ReadSByte();
        }

        public override short ReadInt16()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadInt16() : BitConverter.ToInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override int ReadInt32()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadInt32() : BitConverter.ToInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override long ReadInt64()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadInt64() : BitConverter.ToInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override ushort ReadUInt16()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadUInt16() : BitConverter.ToUInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override uint ReadUInt32()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadUInt32() : BitConverter.ToUInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override ulong ReadUInt64()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadUInt64() : BitConverter.ToUInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override bool ReadBoolean()
        {
            Reset();

            return base.ReadBoolean();
        }

        public override char ReadChar()
        {
            Reset();

            return base.ReadChar();
        }

        public override char[] ReadChars(int count)
        {
            Reset();

            return base.ReadChars(count);
        }

        public override float ReadSingle()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadSingle() : BitConverter.ToSingle(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override double ReadDouble()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadDouble() : BitConverter.ToDouble(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override decimal ReadDecimal()
        {
            Reset();

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadDecimal() : DecimalExtensions.ToDecimal(ReadBytes(16).Reverse().ToArray());
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            Reset();

            return base.Read(buffer, index, count);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            Reset();

            return base.Read(buffer, index, count);
        }

        public override byte[] ReadBytes(int count)
        {
            Reset();

            return base.ReadBytes(count);
        }

        public override string ReadString()
        {
            var length = ReadByte();
            return ReadString(length);
        }

        #endregion

        #region String Reads

        public string ReadCStringASCII() => string.Concat(Enumerable.Range(0, 999).Select(_ => (char)ReadByte()).TakeWhile(c => c != 0));
        public string ReadCStringUTF16() => string.Concat(Enumerable.Range(0, 999).Select(_ => (char)ReadInt16()).TakeWhile(c => c != 0));
        public string ReadCStringSJIS() => Encoding.GetEncoding("Shift-JIS").GetString(Enumerable.Range(0, 999).Select(_ => ReadByte()).TakeWhile(c => c != 0).ToArray());

        public string ReadASCIIStringUntil(byte stop)
        {
            var result = string.Empty;

            var b = ReadByte();
            while (b != stop && BaseStream.Position < BaseStream.Length)
            {
                result += (char)b;
                b = ReadByte();
            }

            return result;
        }

        public string ReadString(int length)
        {
            return ReadString(length, _encoding);
        }

        public string ReadString(int length, Encoding encoding)
        {
            return encoding.GetString(ReadBytes(length));
        }

        public string PeekString(int length = 4)
        {
            return PeekString(0, length, _encoding);
        }

        public string PeekString(int length, Encoding encoding)
        {
            return PeekString(0, length, encoding);
        }

        public string PeekString(long offset, int length = 4)
        {
            return PeekString(offset, length, _encoding);
        }

        public string PeekString(long offset, int length, Encoding encoding)
        {
            var startOffset = BaseStream.Position;

            BaseStream.Seek(offset, SeekOrigin.Current);
            var bytes = ReadBytes(length);

            BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return encoding.GetString(bytes);
        }

        #endregion

        #region Alignment Reads

        public byte SeekAlignment(int alignment = 16)
        {
            var remainder = BaseStream.Position % alignment;
            if (remainder <= 0) return 0;

            var alignmentByte = ReadByte();
            BaseStream.Position += alignment - remainder - 1;

            return alignmentByte;
        }

        #endregion

        #region Helpers

        private void Reset()
        {
            ResetBitBuffer();
            ResetNibbleBuffer();
        }

        public void ResetBitBuffer()
        {
            _bitPosition = 64;
            _buffer = 0;
        }

        public void ResetNibbleBuffer()
        {
            _nibble = -1;
        }

        private void FillBuffer()
        {
            _currentBlockSize = _blockSize;
            switch (_blockSize)
            {
                case 1:
                    _buffer = ReadByte();
                    break;
                case 2:
                    _buffer = ReadInt16();
                    break;
                case 4:
                    _buffer = ReadInt32();
                    break;
                case 8:
                    _buffer = ReadInt64();
                    break;
            }
            _bitPosition = 0;
        }

        #endregion

        #region Read generic type

        public T ReadType<T>() => (T)ReadType(typeof(T));

        public List<T> ReadMultiple<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadType<T>()).ToList();

        private object ReadType(Type type, MemberInfo fieldInfo = null, List<(string name, object value)> readVals = null, string currentNest = "", bool isTypeChosen = false)
        {
            object returnValue;

            if (readVals == null)
                readVals = new List<(string, object)>();

            var typeAttributes = new MemberAttributeInfo(type);
            MemberAttributeInfo fieldAttributes = null;
            if (fieldInfo != null) fieldAttributes = new MemberAttributeInfo(fieldInfo);

            var bkByteOrder = ByteOrder;
            var bkBitOrder = BitOrder;
            var bkBlockSize = _blockSize;

            ByteOrder = fieldAttributes?.EndiannessAttribute?.ByteOrder ?? typeAttributes.EndiannessAttribute?.ByteOrder ?? ByteOrder;

            if (IsTypeChoice(fieldAttributes) && !isTypeChosen)
            {
                returnValue = ReadTypeChoice(type, fieldInfo, readVals, currentNest);
            }
            else if (type.IsPrimitive)
            {
                returnValue = ReadPrimitive(type);
            }
            else if (type == typeof(string))
            {
                returnValue = ReadTypeString(fieldAttributes, readVals);
            }
            else if (type == typeof(decimal))
            {
                returnValue = ReadDecimal();
            }
            else if (Tools.IsList(type))
            {
                returnValue = ReadList(type, fieldInfo, readVals, currentNest);
            }
            else if (type.IsClass || Tools.IsStruct(type))
            {
                returnValue = ReadObject(type, fieldInfo, readVals, currentNest);
            }
            else if (type.IsEnum)
            {
                returnValue = ReadType(type.GetEnumUnderlyingType());
            }
            else throw new UnsupportedTypeException(type);

            ByteOrder = bkByteOrder;
            BitOrder = bkBitOrder;
            _blockSize = bkBlockSize;

            return returnValue;
        }

        private bool IsTypeChoice(MemberAttributeInfo fieldAttributes)
        {
            return fieldAttributes?.TypeChoiceAttributes != null && fieldAttributes.TypeChoiceAttributes.Any();
        }

        private object ReadTypeChoice(Type type, MemberInfo fieldInfo, List<(string name, object value)> readVals, string currentNest)
        {
            var fieldAttributes = new MemberAttributeInfo(fieldInfo);
            var typeChoices = fieldAttributes.TypeChoiceAttributes;

            if (type != typeof(object) && typeChoices.Any(x => !type.IsAssignableFrom(x.InjectionType)))
                throw new InvalidOperationException($"Not all type choices are injectable to {type.Name}");

            Type chosenType = null;
            foreach (var choice in fieldAttributes.TypeChoiceAttributes)
            {
                var value = readVals.FirstOrDefault(x => x.name == choice.FieldName).value;
                if (value == null)
                    throw new InvalidOperationException($"Field {choice.FieldName} could not be found");

                switch (choice.Comparer)
                {
                    case TypeChoiceComparer.Equal:
                        if (Convert.ToUInt64(value) == Convert.ToUInt64(choice.Value))
                            chosenType = choice.InjectionType;
                        break;
                    case TypeChoiceComparer.Greater:
                        if (Convert.ToUInt64(value) > Convert.ToUInt64(choice.Value))
                            chosenType = choice.InjectionType;
                        break;
                    case TypeChoiceComparer.Smaller:
                        if (Convert.ToUInt64(value) < Convert.ToUInt64(choice.Value))
                            chosenType = choice.InjectionType;
                        break;
                    case TypeChoiceComparer.GEqual:
                        if (Convert.ToUInt64(value) >= Convert.ToUInt64(choice.Value))
                            chosenType = choice.InjectionType;
                        break;
                    case TypeChoiceComparer.SEqual:
                        if (Convert.ToUInt64(value) <= Convert.ToUInt64(choice.Value))
                            chosenType = choice.InjectionType;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown comparer {choice.Comparer}");
                }

                if (chosenType != null)
                    break;
            }

            if (chosenType == null)
                throw new InvalidOperationException($"No choice matched the criteria for injection");

            return ReadType(chosenType, fieldInfo, readVals, currentNest, true);
        }

        private object ReadPrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return ReadBoolean();
                case TypeCode.Byte: return ReadByte();
                case TypeCode.SByte: return ReadSByte();
                case TypeCode.Int16: return ReadInt16();
                case TypeCode.UInt16: return ReadUInt16();
                case TypeCode.Char: return ReadChar();
                case TypeCode.Int32: return ReadInt32();
                case TypeCode.UInt32: return ReadUInt32();
                case TypeCode.Int64: return ReadInt64();
                case TypeCode.UInt64: return ReadUInt64();
                case TypeCode.Single: return ReadSingle();
                case TypeCode.Double: return ReadDouble();
                default: throw new NotSupportedException("Unsupported Primitive");
            }
        }

        private string ReadTypeString(MemberAttributeInfo fieldAttributes, List<(string name, object value)> readVals)
        {
            var fixedLengthAttribute = fieldAttributes?.FixedLengthAttribute;
            var variableLengthAttribute = fieldAttributes?.VariableLengthAttribute;

            if (fixedLengthAttribute == null && variableLengthAttribute == null)
                return null;

            var enc = Tools.RetrieveEncoding(fixedLengthAttribute?.StringEncoding ?? variableLengthAttribute.StringEncoding);

            var matchingVals = readVals?.Where(v => v.Item1 == variableLengthAttribute?.FieldName);
            var length = fixedLengthAttribute?.Length ?? ((matchingVals?.Any() ?? false) ? Convert.ToInt32(matchingVals.First().Item2) + variableLengthAttribute.Offset : -1);

            return ReadString(length, enc);
        }

        private object ReadList(Type type, MemberInfo fieldInfo, List<(string name, object value)> readVals, string currentNest)
        {
            MemberAttributeInfo fieldAttributes = null;
            if (fieldInfo != null) fieldAttributes = new MemberAttributeInfo(fieldInfo);

            var fixedLengthAttribute = fieldAttributes?.FixedLengthAttribute;
            var variableLengthAttribute = fieldAttributes?.VariableLengthAttribute;

            var matchingVal = readVals?.FirstOrDefault(v => v.Item1 == variableLengthAttribute?.FieldName).Item2;
            var length = fixedLengthAttribute?.Length ?? ((matchingVal != null) ? Convert.ToInt32(matchingVal) + variableLengthAttribute.Offset : -1);
            if (length <= -1)
                return null;

            IList list;
            Type elementType;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                list = Array.CreateInstance(elementType, length);
            }
            else
            {
                elementType = type.GetGenericArguments()[0];
                list = (IList)Activator.CreateInstance(type);
            }

            for (int i = 0; i < length; i++)
            {
                var elementValue = ReadType(elementType, null, readVals, currentNest, false);
                if (list.IsFixedSize)
                    list[i] = elementValue;
                else
                    list.Add(elementValue);
            }

            return list;
        }

        private object ReadObject(Type type, MemberInfo fieldInfo, List<(string name, object value)> readVals, string currentNest)
        {
            var typeAttributes = new MemberAttributeInfo(type);

            var bitFieldInfoAttribute = typeAttributes.BitFieldInfoAttribute;
            var alignmentAttribute = typeAttributes.AlignmentAttribute;

            BitOrder = (bitFieldInfoAttribute?.BitOrder != BitOrder.Default ? bitFieldInfoAttribute?.BitOrder : BitOrder) ?? BitOrder;
            _blockSize = bitFieldInfoAttribute?.BlockSize ?? _blockSize;
            if (_blockSize != 8 && _blockSize != 4 && _blockSize != 2 && _blockSize != 1)
                throw new InvalidBitFieldInfoException(_blockSize);

            var item = Activator.CreateInstance(type);

            foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
            {
                var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();

                var fieldName = string.IsNullOrEmpty(currentNest) ? field.Name : string.Join(".", currentNest, field.Name);
                object val;
                if (bitInfo != null)
                    val = Convert.ChangeType(ReadBits(bitInfo.BitLength), field.FieldType);
                else
                {
                    val = ReadType(field.FieldType, field.CustomAttributes.Any() ? field : null, readVals, fieldName);
                }

                readVals.Add((fieldName, val));
                field.SetValue(item, val);
            }

            // Apply alignment
            if (alignmentAttribute != null)
            {
                Reset();

                var remainder = BaseStream.Position % alignmentAttribute.Alignment;
                if (remainder > 0)
                    BaseStream.Position += alignmentAttribute.Alignment - remainder;

                //BaseStream.Position += alignmentAttribute.Alignment - BaseStream.Position % alignmentAttribute.Alignment;
            }

            return item;
        }

        #endregion

        #region Custom Methods

        public int ReadNibble()
        {
            ResetBitBuffer();

            if (_nibble == -1)
            {
                _nibble = ReadByte();
                return NibbleOrder == NibbleOrder.LowNibbleFirst ?
                    _nibble % 16 :
                    _nibble / 16;
            }

            var val = NibbleOrder == NibbleOrder.LowNibbleFirst ?
                _nibble / 16 :
                _nibble % 16;

            _nibble = -1;
            return val;
        }

        public byte[] ScanBytes(int pos, int length = 1)
        {
            var startOffset = BaseStream.Position;

            if (pos + length >= BaseStream.Length) length = (int)BaseStream.Length - pos;
            if (pos < 0 || pos >= BaseStream.Length) pos = length = 0;

            BaseStream.Position = pos;
            var result = ReadBytes(length);
            BaseStream.Position = startOffset;

            return result;
        }

        public byte[] ReadAllBytes()
        {
            var startOffset = BaseStream.Position;

            BaseStream.Position = 0;
            var output = ReadBytes((int)BaseStream.Length);
            BaseStream.Position = startOffset;

            return output;
        }

        public byte[] ReadBytesUntil(byte stop)
        {
            var result = new List<byte>();

            var b = ReadByte();
            while (b != stop && BaseStream.Position < BaseStream.Length)
            {
                result.Add(b);
                b = ReadByte();
            }

            return result.ToArray();
        }

        // Bit Fields
        public bool ReadBit()
        {
            ResetNibbleBuffer();

            if (_bitPosition >= _currentBlockSize * 8)
                FillBuffer();

            switch (EffectiveBitOrder)
            {
                case BitOrder.LeastSignificantBitFirst:
                    return ((_buffer >> _bitPosition++) & 0x1) == 1;
                case BitOrder.MostSignificantBitFirst:
                    return ((_buffer >> (_currentBlockSize * 8 - _bitPosition++ - 1)) & 0x1) == 1;
                default:
                    throw new NotSupportedException("BitOrder not supported.");
            }
        }

        public object ReadBits(int count)
        {
            long result = 0;
            for (var i = 0; i < count; i++)
                switch (EffectiveBitOrder)
                {
                    case BitOrder.LeastSignificantBitFirst:
                        result |= (ReadBit() ? 1 : 0) << i;
                        break;
                    case BitOrder.MostSignificantBitFirst:
                        result <<= 1;
                        result |= ReadBit() ? 1 : 0;
                        break;
                }

            return result;
        }

        public T ReadBits<T>(int count)
        {
            if (typeof(T) != typeof(bool) &&
                typeof(T) != typeof(sbyte) && typeof(T) != typeof(byte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong))
                throw new UnsupportedTypeException(typeof(T));

            var value = ReadBits(count);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion
    }
}