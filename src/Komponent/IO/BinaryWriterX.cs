using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using Komponent.IO.Attributes;

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

        #endregion

        #region Generic type writing

        public void WriteType<T>(T obj) => WriteType(obj, null, null);

        public void WriteMultiple<T>(IEnumerable<T> list)
        {
            foreach (var element in list)
                WriteType(element);
        }

        private void WriteType(object data, MemberInfo fieldInfo, List<(string name, object value)> wroteVals, string currentNest = "", bool isTypeChosen = false)
        {
            var type = data.GetType();

            if (wroteVals == null)
                wroteVals = new List<(string, object)>();

            var typeAttributes = new MemberAttributeInfo(type);
            MemberAttributeInfo fieldAttributes = null;
            if (fieldInfo != null) fieldAttributes = new MemberAttributeInfo(fieldInfo);

            var bkByteOrder = ByteOrder;
            var bkBitOrder = BitOrder;
            var bkBlockSize = _blockSize;

            ByteOrder = fieldAttributes?.EndiannessAttribute?.ByteOrder ?? typeAttributes.EndiannessAttribute?.ByteOrder ?? ByteOrder;

            if (type.IsPrimitive)
            {
                WritePrimitive(data);
            }
            else if (type == typeof(string))
            {
                WriteTypeString(data, fieldAttributes, wroteVals);
            }
            else if (type == typeof(decimal))
            {
                Write((decimal)data);
            }
            else if (Tools.IsList(type))
            {
                WriteList(data, fieldAttributes, wroteVals);
            }
            else if (type.IsClass || Tools.IsStruct(type))
            {
                WriteObject(data, fieldInfo, wroteVals, currentNest);
            }
            else if (type.IsEnum)
            {
                WriteType((type as TypeInfo)?.DeclaredFields.ToList()[0].GetValue(data));
            }
            else throw new UnsupportedTypeException(type);

            ByteOrder = bkByteOrder;
            BitOrder = bkBitOrder;
            _blockSize = bkBlockSize;
        }

        private void WritePrimitive(object data)
        {
            switch (Type.GetTypeCode(data.GetType()))
            {
                case TypeCode.Boolean: Write((bool)data); break;
                case TypeCode.Byte: Write((byte)data); break;
                case TypeCode.SByte: Write((sbyte)data); break;
                case TypeCode.Int16: Write((short)data); break;
                case TypeCode.UInt16: Write((ushort)data); break;
                case TypeCode.Char: Write((char)data); break;
                case TypeCode.Int32: Write((int)data); break;
                case TypeCode.UInt32: Write((uint)data); break;
                case TypeCode.Int64: Write((long)data); break;
                case TypeCode.UInt64: Write((ulong)data); break;
                case TypeCode.Single: Write((float)data); break;
                case TypeCode.Double: Write((double)data); break;
                default: throw new NotSupportedException("Unsupported Primitive");
            }
        }

        private void WriteTypeString(object data, MemberAttributeInfo fieldAttributes, List<(string name, object value)> wroteVals)
        {
            var fixedSizeAttribute = fieldAttributes?.FixedLengthAttribute;
            var variableSizeAttribute = fieldAttributes?.VariableLengthAttribute;

            if (fixedSizeAttribute == null && variableSizeAttribute == null)
                return;

            var enc = Tools.RetrieveEncoding(fixedSizeAttribute?.StringEncoding ?? variableSizeAttribute.StringEncoding);

            var matchingVals = wroteVals?.Where(v => v.Item1 == variableSizeAttribute?.FieldName);
            var length = fixedSizeAttribute?.Length ?? ((matchingVals?.Any() ?? false) ? Convert.ToInt32(matchingVals.First().Item2) + variableSizeAttribute.Offset : -1);

            if (enc.GetByteCount((string)data) != length)
                throw new FieldLengthMismatchException(enc.GetByteCount((string)data), length);

            WriteString((string)data, enc, false, false);
        }

        private void WriteList(object data, MemberAttributeInfo fieldAttributes, List<(string name, object value)> wroteVals)
        {
            var fixedSizeAttribute = fieldAttributes?.FixedLengthAttribute;
            var variableSizeAttribute = fieldAttributes?.VariableLengthAttribute;

            if (fixedSizeAttribute == null && variableSizeAttribute == null)
                return;

            var matchingVal = wroteVals?.FirstOrDefault(v => v.Item1 == variableSizeAttribute?.FieldName).Item2;
            var length = fixedSizeAttribute?.Length ?? ((matchingVal != null) ? Convert.ToInt32(matchingVal) + variableSizeAttribute.Offset : -1);

            var list = (IList)data;

            if (list.Count != length)
                throw new FieldLengthMismatchException(list.Count, length);

            foreach (var element in list)
                WriteType(element);
        }

        private void WriteObject(object data, MemberInfo fieldInfo, List<(string name, object value)> wroteVals, string currentNest)
        {
            var typeAttributes = new MemberAttributeInfo(data.GetType());

            var bitFieldInfoAttribute = typeAttributes.BitFieldInfoAttribute;
            var alignmentAttribute = typeAttributes.AlignmentAttribute;

            BitOrder = (bitFieldInfoAttribute?.BitOrder != BitOrder.Inherit ? bitFieldInfoAttribute?.BitOrder : BitOrder) ?? BitOrder;
            _blockSize = bitFieldInfoAttribute?.BlockSize ?? _blockSize;
            if (_blockSize != 8 && _blockSize != 4 && _blockSize != 2 && _blockSize != 1)
                throw new InvalidBitFieldInfoException(_blockSize);

            foreach (var field in data.GetType().GetFields().OrderBy(fi => fi.MetadataToken))
            {
                var fieldName = string.IsNullOrEmpty(currentNest) ? field.Name : string.Join(".", currentNest, field.Name);
                wroteVals.Add((fieldName, field.GetValue(data)));

                var bitInfo = field.GetCustomAttribute<BitFieldAttribute>();
                if (bitInfo != null)
                {
                    WriteBits(Convert.ToInt64(field.GetValue(data)), bitInfo.BitLength);
                }
                else
                    WriteType(field.GetValue(data), field.CustomAttributes.Any() ? field : null, wroteVals, fieldName);
            }

            Flush();

            // Apply alignment
            if (alignmentAttribute != null)
                Write(new byte[alignmentAttribute.Alignment - BaseStream.Position % alignmentAttribute.Alignment]);
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

        #endregion
    }
}