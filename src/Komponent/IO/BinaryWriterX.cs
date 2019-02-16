using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace Komponent.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _nibble = -1;
        private int _blockSize;
        private int _bitPosition = 0;
        private long _buffer;

        public ByteOrder ByteOrder { get; set; }
        public BitOrder BitOrder { get; set; }

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
            get => _blockSize;
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 8)
                    throw new Exception("BlockSize can only be 1, 2, 4, or 8.");
                _blockSize = value;
            }
        }

        #region Constructors

        public BinaryWriterX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, Encoding.UTF8)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryWriterX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, Encoding.UTF8, leaveOpen)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
        }

        public BinaryWriterX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, encoding)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
            _encoding = encoding;
        }

        public BinaryWriterX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MSBFirst, int blockSize = 4) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;
            _encoding = encoding;
        }

        #endregion

        #region Default Writes

        public override void Flush()
        {
            FlushBuffer();
            FlushNibble();

            base.Flush();
        }

        public override void Write(byte value)
        {
            Flush();

            base.Write(value);
        }

        public override void Write(sbyte value)
        {
            Flush();

            base.Write(value);
        }

        public override void Write(short value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(int value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(long value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ushort value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(uint value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ulong value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(bool value)
        {
            Flush();

            base.Write(value);
        }

        public override void Write(char value)
        {
            Flush();

            base.Write(value);
        }

        public override void Write(char[] chars)
        {
            Flush();

            base.Write(chars);
        }

        public override void Write(float value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(double value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(decimal value)
        {
            Flush();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(DecimalExtensions.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(byte[] buffer)
        {
            Flush();

            base.Write(buffer);
        }

        public override void Write(byte[] buffer, int index, int count)
        {
            Flush();

            base.Write(buffer, index, count);
        }

        public override void Write(char[] chars, int index, int count)
        {
            Flush();

            base.Write(chars, index, count);
        }

        public override void Write(string value)
        {
            Flush();

            WriteString(value, _encoding, true, false);
        }

        #endregion

        #region String Writes

        public void WriteString(string value, Encoding encoding, bool leadingCount = true, bool nullTerminator = true)
        {
            if (nullTerminator)
                value += "\0";

            var bytes = encoding.GetBytes(value);

            if (leadingCount)
                Write((byte)bytes.Length);

            Write(bytes);
        }

        #endregion

        #region Alignement & Padding Writes

        public void WritePadding(int count, byte paddingByte = 0x0)
        {
            for (var i = 0; i < count; i++)
                Write(paddingByte);
        }

        public void WriteAlignment(int alignment = 16, byte alignmentByte = 0x0)
        {
            var remainder = BaseStream.Position % alignment;
            if (remainder <= 0) return;
            for (var i = 0; i < alignment - remainder; i++)
                Write(alignmentByte);
        }

        public void WriteAlignment(byte alignmentByte) => WriteAlignment(16, alignmentByte);

        #endregion

        #region Helpers

        private void FlushBuffer()
        {
            if (_bitPosition <= 0)
                return;

            _bitPosition = 0;
            switch (_blockSize)
            {
                case 1:
                    Write((byte)_buffer);
                    break;
                case 2:
                    Write((short)_buffer);
                    break;
                case 4:
                    Write((int)_buffer);
                    break;
                case 8:
                    Write(_buffer);
                    break;
            }
            _buffer = 0;
        }

        private void FlushNibble()
        {
            if (_nibble != -1)
            {
                var value = _nibble;
                _nibble = -1;
                Write((byte)value);
            }
        }

        private void WritePrimitive(object obj)
        {
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean: Write((bool)obj); break;
                case TypeCode.Byte: Write((byte)obj); break;
                case TypeCode.SByte: Write((sbyte)obj); break;
                case TypeCode.Int16: Write((short)obj); break;
                case TypeCode.UInt16: Write((ushort)obj); break;
                case TypeCode.Char: Write((char)obj); break;
                case TypeCode.Int32: Write((int)obj); break;
                case TypeCode.UInt32: Write((uint)obj); break;
                case TypeCode.Int64: Write((long)obj); break;
                case TypeCode.UInt64: Write((ulong)obj); break;
                case TypeCode.Single: Write((float)obj); break;
                case TypeCode.Double: Write((double)obj); break;
                default: throw new NotSupportedException("Unsupported Primitive");
            }
        }

        private void WriteObject(object obj, MemberInfo fieldInfo = null, List<(string, object)> wroteVals = null)
        {
            var type = obj.GetType();

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
                WritePrimitive(obj);
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

                    var matchingVals = wroteVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);

                    if (enc.GetByteCount((string)obj) != length)
                        throw new FieldLengthMismatchException(enc.GetByteCount((string)obj), length);

                    WriteString((string)obj, enc, false, false);
                }
            }
            else if (Type.GetTypeCode(type) == TypeCode.Decimal)
            {
                // Decimal
                Write((decimal)obj);
            }
            else if (type.IsArray)
            {
                // Array
                if (FixedSize != null || VarSize != null)
                {
                    var matchingVals = wroteVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);
                    var arr = (obj as Array);

                    if (arr.Length != length)
                        throw new FieldLengthMismatchException(arr.Length, length);

                    foreach (var element in arr)
                        WriteObject(element);
                }
            }
            else if (type.IsGenericType && type.Name.Contains("List"))
            {
                // List
                if (FixedSize != null || VarSize != null)
                {
                    var matchingVals = wroteVals.Where(v => v.Item1 == VarSize.FieldName);
                    var length = FixedSize?.Length ?? (matchingVals.Any() ? Convert.ToInt32(matchingVals.First().Item2) + VarSize.Offset : -1);
                    var list = (obj as IList);

                    if (list.Count != length)
                        throw new FieldLengthMismatchException(list.Count, length);

                    foreach (var element in list)
                        WriteObject(element);
                }
            }
            else if (type.IsClass || (type.IsValueType && !type.IsEnum))
            {
                // Class/Struct
                BitOrder = (BitFieldInfo?.BitOrder != BitOrder.Inherit ? BitFieldInfo?.BitOrder : BitOrder) ?? BitOrder;
                _blockSize = BitFieldInfo?.BlockSize ?? _blockSize;
                if (_blockSize != 8 && _blockSize != 4 && _blockSize != 2 && _blockSize != 1)
                    throw new InvalidBitFieldInfoException(_blockSize);

                var wroteValsIntern = new List<(string, object)>();
                foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                {
                    wroteValsIntern.Add((field.Name, field.GetValue(obj)));

                    var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();
                    if (bitInfo != null)
                    {
                        WriteBits(Convert.ToInt64(field.GetValue(obj)), bitInfo.BitLength);
                    }
                    else
                        WriteObject(field.GetValue(obj), field.CustomAttributes.Any() ? field : null, wroteValsIntern);
                }

                Flush();
            }
            else if (type.IsEnum)
            {
                // Enum
                WriteObject((type as TypeInfo)?.DeclaredFields.ToList()[0].GetValue(obj));
            }
            else throw new UnsupportedTypeException(type);

            ByteOrder = bkByteOrder;
            BitOrder = bkBitOrder;
            _blockSize = bkBlockSize;
        }

        #endregion

        #region Custom Methods

        public void WriteNibble(int val)
        {
            FlushBuffer();

            val &= 15;
            if (_nibble == -1)
                _nibble = val;
            else
            {
                _nibble += 16 * val;
                FlushNibble();
            }
        }

        // Bit Fields
        public void WriteBit(bool value)
        {
            FlushNibble();

            if (EffectiveBitOrder == BitOrder.LSBFirst)
                _buffer |= ((value) ? 1 : 0) << _bitPosition++;
            else
                _buffer |= ((value) ? 1 : 0) << (BlockSize * 8 - _bitPosition++ - 1);

            if (_bitPosition >= BlockSize * 8)
                Flush();
        }

        private void WriteBit(bool value, bool writeBuffer)
        {
            FlushNibble();

            if (EffectiveBitOrder == BitOrder.LSBFirst)
                _buffer |= ((value) ? 1 : 0) << _bitPosition++;
            else
                _buffer |= ((value) ? 1 : 0) << (BlockSize * 8 - _bitPosition++ - 1);

            if (writeBuffer)
                Flush();
        }

        public void WriteBits(long value, int bitCount)
        {
            FlushNibble();

            if (bitCount > 0)
            {
                if (EffectiveBitOrder == BitOrder.LSBFirst)
                {
                    for (int i = 0; i < bitCount; i++)
                    {
                        WriteBit((value & 1) == 1, _bitPosition + 1 >= BlockSize * 8);
                        value >>= 1;
                    }
                }
                else
                {
                    for (int i = bitCount - 1; i >= 0; i--)
                    {
                        WriteBit(((value >> i) & 1) == 1, _bitPosition + 1 >= BlockSize * 8);
                    }
                }
            }
            else
            {
                throw new Exception("BitCount needs to be greater than 0");
            }
        }

        public void WriteStruct<T>(T obj) => WriteObject(obj);

        public void WriteMultiple<T>(IEnumerable<T> list)
        {
            foreach (var element in list)
                WriteStruct(element);
        }

        #endregion
    }
}