using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;

namespace Komponent.IO
{
    public class BinaryReaderX : BinaryReader
    {
        private int _nibble = -1;

        private long buffer = 0;
        private int bitPosition = 64;

        public ByteOrder ByteOrder { get; set; }

        public BitOrder BitOrder { get; set; }
        public BitOrder EffectiveBitOrder
        {
            get
            {
                if ((ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.LowestAddressFirst) || (ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.HighestAddressFirst))
                    return BitOrder.LSBFirst;
                else if ((ByteOrder == ByteOrder.LittleEndian && BitOrder == BitOrder.HighestAddressFirst) || (ByteOrder == ByteOrder.BigEndian && BitOrder == BitOrder.LowestAddressFirst))
                    return BitOrder.MSBFirst;
                else
                    return BitOrder;
            }
        }

        private int _blockSize;
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (value != 8 && value != 16 && value != 32 && value != 64)
                    throw new Exception("BlockSize can only be 8, 16, 32, or 64.");
                _blockSize = value;
            }
        }

        #region Constructors
        public BinaryReaderX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, Encoding.Unicode)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        public BinaryReaderX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, encoding)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        public BinaryReaderX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        // Parameters out of order with a default encoding of Unicode
        public BinaryReaderX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, Encoding.Unicode, leaveOpen)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }
        #endregion

        #region Default Reads
        public override byte ReadByte()
        {
            ResetBuffer();

            return base.ReadByte();
        }

        public override short ReadInt16()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt16();
            return BitConverter.ToInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override int ReadInt32()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt32();
            return BitConverter.ToInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override long ReadInt64()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt64();
            return BitConverter.ToInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override ushort ReadUInt16()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt16();
            return BitConverter.ToUInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override uint ReadUInt32()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt32();
            return BitConverter.ToUInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override ulong ReadUInt64()
        {
            ResetBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt64();
            return BitConverter.ToUInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override bool ReadBoolean()
        {
            ResetBuffer();

            return base.ReadBoolean();
        }

        public override char ReadChar()
        {
            ResetBuffer();

            return base.ReadChar();
        }

        public override char[] ReadChars(int count)
        {
            ResetBuffer();

            return base.ReadChars(count);
        }

        public override float ReadSingle()
        {
            return base.ReadSingle();
        }

        public override double ReadDouble()
        {
            ResetBuffer();

            return base.ReadDouble();
        }

        public override decimal ReadDecimal()
        {
            ResetBuffer();

            return base.ReadDecimal();
        }

        public override sbyte ReadSByte()
        {
            ResetBuffer();

            return base.ReadSByte();
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
            return Encoding.ASCII.GetString(ReadBytes(length)).TrimEnd('\0');
        }

        public string ReadString(int length, Encoding encoding)
        {
            return encoding.GetString(ReadBytes(length)).TrimEnd('\0');
        }

        public string PeekString(int length = 4)
        {
            var bytes = new List<byte>();
            var startOffset = BaseStream.Position;

            for (var i = 0; i < length; i++)
                bytes.Add(ReadByte());

            BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        public string PeekString(int length, Encoding encoding)
        {
            var bytes = new List<byte>();
            var startOffset = BaseStream.Position;

            for (var i = 0; i < length; i++)
                bytes.Add(ReadByte());

            BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return encoding.GetString(bytes.ToArray());
        }

        public string PeekString(uint offset, int length = 4)
        {
            var bytes = new List<byte>();
            var startOffset = BaseStream.Position;

            BaseStream.Seek(offset, SeekOrigin.Begin);
            for (var i = 0; i < length; i++)
                bytes.Add(ReadByte());

            BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        public string PeekString(uint offset, int length, Encoding encoding)
        {
            var bytes = new List<byte>();
            var startOffset = BaseStream.Position;

            BaseStream.Seek(offset, SeekOrigin.Begin);
            for (var i = 0; i < length; i++)
                bytes.Add(ReadByte());

            BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return encoding.GetString(bytes.ToArray());
        }
        #endregion

        #region Alignement Reads
        public byte SeekAlignment(int alignment = 16, byte alignmentByte = 0x0)
        {
            var remainder = BaseStream.Position % alignment;
            if (remainder <= 0) return alignmentByte;
            alignmentByte = ReadByte();
            BaseStream.Seek(-1, SeekOrigin.Current);
            BaseStream.Seek(16 - remainder, SeekOrigin.Current);

            return alignmentByte;
        }

        public byte SeekAlignment(byte alignmentByte, int alignment = 16) => SeekAlignment(alignment, alignmentByte);
        #endregion

        #region Helpers
        public void ResetBuffer()
        {
            bitPosition = 64;
            buffer = 0;
        }

        public void FillBuffer()
        {
            switch (_blockSize)
            {
                case 8:
                    buffer = ReadByte();
                    break;
                case 16:
                    buffer = ReadInt16();
                    break;
                case 32:
                    buffer = ReadInt32();
                    break;
                case 64:
                    buffer = ReadInt64();
                    break;
            }
            bitPosition = 0;
        }

        object GetPrimitive(Type type)
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

        object GetObject(Type type, FieldInfo fieldInfo = null)
        {
            if (type.IsPrimitive)
            {
                return GetPrimitive(type);
            }
            else
            {
                if (Type.GetTypeCode(type) == TypeCode.String)
                {
                    //string
                    var fieldSize = fieldInfo.GetCustomAttribute<Length>();
                    if (fieldSize != null)
                        return ReadString(fieldSize._length);
                    else return null;
                }
                else if (Type.GetTypeCode(type) == TypeCode.Decimal)
                {
                    //decimal
                    return ReadDecimal();
                }
                else if (type.IsArray)
                {
                    //array
                    //get endianness attriute
                    var bk_ByteOrder = ByteOrder;
                    var endian = type.GetCustomAttribute<Endianness>();
                    if (endian != null)
                        ByteOrder = endian.ByteOrder;

                    var fieldSize = fieldInfo.GetCustomAttribute<Length>();
                    if (fieldSize != null)
                    {
                        IList arr = Array.CreateInstance(type.GetElementType(), fieldSize._length);
                        for (int i = 0; i < fieldSize._length; i++)
                            arr[i] = GetObject(type.GetElementType(), null);
                        ByteOrder = bk_ByteOrder;
                        return arr;
                    }
                    else
                    {
                        ByteOrder = bk_ByteOrder;
                        return null;
                    }
                }
                else if (type.IsClass || (type.IsValueType && !type.IsEnum))
                {
                    //class, struct

                    //get endianness attriute
                    var bk_ByteOrder = ByteOrder;
                    var endian = type.GetCustomAttribute<Endianness>();
                    if (endian != null)
                        ByteOrder = endian.ByteOrder;

                    //get bitfieldblock attribute
                    var block = type.GetCustomAttribute<BitFieldInfo>();
                    if (block != null)
                    {
                        BlockSize = block.BlockSize;
                        BitOrder = block.Orientation;

                        var item = Activator.CreateInstance(type);

                        foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                        {
                            var bitInfo = field.GetCustomAttribute<BitField>();
                            if (bitInfo != null)
                                field.SetValue(item, ReadBits(bitInfo._bitsToRead));
                            else
                                field.SetValue(item, GetObject(field.FieldType, (field.CustomAttributes.Count() > 0) ? field : null));
                        }

                        ByteOrder = bk_ByteOrder;
                        return item;
                    }
                    else
                    {
                        var item = Activator.CreateInstance(type);

                        foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                            field.SetValue(item, GetObject(field.FieldType, (field.CustomAttributes.Count() > 0) ? field : null));

                        ByteOrder = bk_ByteOrder;
                        return item;
                    }
                }
                else if (type.IsEnum)
                {
                    //enum

                    //get endianness attriute
                    var bk_ByteOrder = ByteOrder;
                    var endian = type.GetCustomAttribute<Endianness>();
                    if (endian != null)
                        ByteOrder = endian.ByteOrder;

                    var item = GetObject(type.GetEnumUnderlyingType());
                    ByteOrder = bk_ByteOrder;
                    return item;
                }
                else return null;
            }
        }
        #endregion

        #region Custom Methods
        public int ReadNibble()
        {
            ResetBuffer();

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
            var startOffset = base.BaseStream.Position;

            if (pos + length >= base.BaseStream.Length) length = (int)base.BaseStream.Length - pos;
            if (pos < 0 || pos >= base.BaseStream.Length) pos = length = 0;

            base.BaseStream.Position = pos;
            var result = base.ReadBytes(length);
            base.BaseStream.Position = startOffset;

            return result;
        }

        public byte[] ReadAllBytes()
        {
            var startOffset = base.BaseStream.Position;

            base.BaseStream.Position = 0;
            var output = base.ReadBytes((int)base.BaseStream.Length);
            base.BaseStream.Position = startOffset;

            return output;
        }

        public byte[] ReadBytesUntil(byte stop)
        {
            List<byte> result = new List<byte>();

            byte b = ReadByte();
            while (b != stop && BaseStream.Position < BaseStream.Length)
            {
                result.Add(b);
                b = ReadByte();
            }

            return result.ToArray();
        }

        public bool ReadBit()
        {
            if (bitPosition >= _blockSize)
                FillBuffer();

            switch (EffectiveBitOrder)
            {
                case BitOrder.LSBFirst:
                    return ((buffer >> bitPosition++) & 0x1) == 1;
                case BitOrder.MSBFirst:
                    return ((buffer >> (_blockSize - bitPosition++ - 1)) & 0x1) == 1;
                default:
                    throw new NotSupportedException("BitOrder not supported.");
            }
        }

        public long ReadBits(int count)
        {
            long result = 0;
            for (int i = 0; i < count; i++)
                switch (EffectiveBitOrder)
                {
                    case BitOrder.LSBFirst:
                        result |= ((ReadBit()) ? 1 : 0) << i;
                        break;
                    case BitOrder.MSBFirst:
                        result <<= 1;
                        result |= (ReadBit()) ? 1 : 0;
                        break;
                }

            return result;
        }

        public T ReadStruct<T>() => (T)GetObject(typeof(T));

        public List<T> ReadMultiple<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();
        #endregion
    }
}