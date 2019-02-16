using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
        public bool IsFirstNibble { get => _nibble == -1; }

        private Encoding _encoding = Encoding.UTF8;

        public BitOrder EffectiveBitOrder
        {
            get
            {
                if (ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.LowestAddressFirst || ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.HighestAddressFirst)
                    return BitOrder.LSBFirst;
                if (ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.HighestAddressFirst || ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.LowestAddressFirst)
                    return BitOrder.MSBFirst;
                return BitOrder;
            }
        }

        public int BlockSize
        {
            get => _currentBlockSize;
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 8)
                    throw new Exception("BlockSize can only be 1, 2, 4, or 8.");
                _blockSize = value;
                _currentBlockSize = value;
            }
        }

        #region Constructors

        public BinaryReaderX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, Encoding.UTF8)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryReaderX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, Encoding.UTF8, leaveOpen)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryReaderX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, encoding)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
            _encoding = encoding;
        }

        public BinaryReaderX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
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

        private object ReadObject(Type type, MemberInfo fieldInfo = null, List<(string, object)> readVals = null)
        {
            object returnValue;

            var TypeEndian = type.GetCustomAttribute<EndiannessAttribute>();
            var FieldEndian = fieldInfo?.GetCustomAttribute<EndiannessAttribute>();
            var FixedSize = fieldInfo?.GetCustomAttribute<FixedLengthAttribute>();
            var VarSize = fieldInfo?.GetCustomAttribute<VariableLengthAttribute>();
            var BitFieldInfo = type.GetCustomAttribute<BitFieldInfoAttribute>();

            var bkByteOrder = ByteOrder;
            var bkBitOrder = BitOrder;
            var bkBlockSize = _blockSize;

            ByteOrder = FieldEndian?.ByteOrder ?? TypeEndian?.ByteOrder ?? ByteOrder;

            if (type.IsPrimitive)
            {
                //Primitive
                returnValue = ReadPrimitive(type);
            }
            else if (Type.GetTypeCode(type) == TypeCode.String)
            {
                // String
                if (FixedSize != null || VarSize != null)
                {
                    var strEnc = FixedSize?.StringEncoding ?? VarSize.StringEncoding;
                    Encoding enc;
                    switch (strEnc)
                    {
                        default:
                        case StringEncoding.ASCII: enc = Encoding.ASCII; break;
                        case StringEncoding.SJIS: enc = Encoding.GetEncoding("SJIS"); break;
                        case StringEncoding.Unicode: enc = Encoding.Unicode; break;
                        case StringEncoding.UTF16: enc = Encoding.Unicode; break;
                        case StringEncoding.UTF32: enc = Encoding.UTF32; break;
                        case StringEncoding.UTF7: enc = Encoding.UTF7; break;
                        case StringEncoding.UTF8: enc = Encoding.UTF8; break;
                    }

                    var matchingVals = readVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);

                    returnValue = ReadString(length, enc);
                }
                else
                    returnValue = null;
            }
            else if (Type.GetTypeCode(type) == TypeCode.Decimal)
            {
                // Decimal
                returnValue = ReadDecimal();
            }
            else if (type.IsArray)
            {
                // Array
                if (FixedSize != null || VarSize != null)
                {
                    var matchingVals = readVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);
                    IList arr = Array.CreateInstance(type.GetElementType(), length);

                    for (int i = 0; i < length; i++)
                        arr[i] = ReadObject(type.GetElementType());

                    returnValue = arr;
                }
                else
                    returnValue = null;
            }
            else if (type.IsGenericType && type.Name.Contains("List"))
            {
                // List
                if (FixedSize != null || VarSize != null)
                {
                    var matchingVals = readVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);
                    var paramType = type.GenericTypeArguments.First();
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { paramType }));

                    for (int i = 0; i < length; i++)
                        list.GetType().GetMethod("Add").Invoke(list, new[] { ReadObject(paramType) });

                    returnValue = list;
                }
                else
                    returnValue = null;
            }
            else if (type.IsClass || type.IsValueType && !type.IsEnum)
            {
                // Class, Struct
                BitOrder = (BitFieldInfo?.BitOrder != BitOrder.Inherit ? BitFieldInfo?.BitOrder : BitOrder) ?? BitOrder;
                _blockSize = BitFieldInfo?.BlockSize ?? _blockSize;
                if (_blockSize != 8 && _blockSize != 4 && _blockSize != 2 && _blockSize != 1)
                    throw new InvalidBitFieldInfoException(_blockSize);

                var readValsIntern = new List<(string, object)>();
                var item = Activator.CreateInstance(type);

                foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                {
                    var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();

                    object val;
                    if (bitInfo != null)
                        val = Convert.ChangeType(ReadBits(bitInfo.BitLength), field.FieldType);
                    else
                        val = ReadObject(field.FieldType, field.CustomAttributes.Any() ? field : null, readValsIntern);

                    readValsIntern.Add((field.Name, val));
                    field.SetValue(item, val);
                }

                returnValue = item;
            }
            else if (type.IsEnum)
            {
                // Enum
                returnValue = ReadObject(type.GetEnumUnderlyingType());
            }
            else throw new UnsupportedTypeException(type);

            ByteOrder = bkByteOrder;
            BitOrder = bkBitOrder;
            _blockSize = bkBlockSize;

            return returnValue;
        }

        #endregion

        #region Custom Methods

        public int ReadNibble()
        {
            ResetBitBuffer();

            if (_nibble == -1)
            {
                _nibble = ReadByte();
                return _nibble % 16;
            }
            var val = _nibble / 16;
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
                case BitOrder.LSBFirst:
                    return ((_buffer >> _bitPosition++) & 0x1) == 1;
                case BitOrder.MSBFirst:
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
                    case BitOrder.LSBFirst:
                        result |= (ReadBit() ? 1 : 0) << i;
                        break;
                    case BitOrder.MSBFirst:
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

        public T ReadStruct<T>() => (T)ReadObject(typeof(T));

        public List<T> ReadMultiple<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();

        #endregion
    }
}