using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace Komponent.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _nibble = -1;

        private long buffer = 0;
        private int bitPosition = 0;

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
        public BinaryWriterX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, Encoding.Unicode)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        public BinaryWriterX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, encoding)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        public BinaryWriterX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }

        // Parameters out of order with a default encoding of Unicode
        public BinaryWriterX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, int blockSize = 32, BitOrder BitOrder = BitOrder.MSBFirst) : base(input, Encoding.Unicode, leaveOpen)
        {
            ByteOrder = byteOrder;
            _blockSize = blockSize;
            this.BitOrder = BitOrder;
        }
        #endregion

        #region Default Writes
        public override void Write(byte value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }

        public override void Write(short value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(int value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(long value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ushort value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(uint value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ulong value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(bool value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }

        public override void Write(char ch)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(ch);
        }

        public override void Write(float value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }

        public override void Write(double value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }

        public override void Write(decimal value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }

        public override void Write(sbyte value)
        {
            if (bitPosition > 0)
                WriteBuffer();

            base.Write(value);
        }
        #endregion

        #region String Writes
        public void WriteASCII(string value)
        {
            base.Write(Encoding.ASCII.GetBytes(value));
        }

        public void WriteString(string value, Encoding encoding, bool leadingCount = true)
        {
            var bytes = encoding.GetBytes(value);
            if (leadingCount)
                Write((byte)bytes.Length);
            Write(bytes);
        }
        #endregion

        #region Alignement/Padding Writes
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
        public void WriteBuffer()
        {
            bitPosition = 0;
            switch (BlockSize)
            {
                case 8:
                    Write((byte)buffer);
                    break;
                case 16:
                    Write((short)buffer);
                    break;
                case 32:
                    Write((int)buffer);
                    break;
                case 64:
                    Write(buffer);
                    break;
            }
            buffer = 0;
        }

        public void WritePrimitive(object obj)
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

        public void WriteObject(object obj)
        {
            var type = obj.GetType();
            if (type.IsPrimitive)
            {
                WritePrimitive(obj);
            }
            else
            {
                if (Type.GetTypeCode(type) == TypeCode.String)
                {
                    //string
                    WriteString((string)obj, Encoding.ASCII, false);
                }
                else if (Type.GetTypeCode(type) == TypeCode.Decimal)
                {
                    //decimal
                    Write((decimal)obj);
                }
                else if (type.IsArray)
                {
                    //array
                    //get endianness attriute
                    var bk_ByteOrder = ByteOrder;
                    var endian = type.GetCustomAttribute<Endianness>();
                    if (endian != null)
                        ByteOrder = endian.ByteOrder;

                    foreach (var element in (Array)obj)
                        WriteObject(element);

                    ByteOrder = bk_ByteOrder;
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

                        foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                        {
                            var bitInfo = field.GetCustomAttribute<BitField>();
                            if (bitInfo != null)
                                WriteBits((long)field.GetValue(obj), bitInfo._bitsToRead);
                            else
                                WriteObject(field.GetValue(obj));
                        }

                        if (bitPosition > 0)
                            WriteBuffer();
                    }
                    else
                    {
                        foreach (var field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                            WriteObject(field.GetValue(obj));
                    }

                    ByteOrder = bk_ByteOrder;
                }
                else if (type.IsEnum)
                {
                    //enum
                    WriteObject((type as TypeInfo).DeclaredFields.ToList()[0].GetValue(obj));
                }
            }
        }
        #endregion

        #region Custom methods
        public void WriteNibble(int val)
        {
            if (bitPosition > 0)
                WriteBuffer();

            val &= 15;
            if (_nibble == -1)
                _nibble = val;
            else
            {
                Write((byte)(_nibble + 16 * val));
                _nibble = -1;
            }
        }

        public void WriteBit(bool value)
        {
            if (EffectiveBitOrder == BitOrder.LSBFirst)
                buffer |= ((value) ? 1 : 0) << bitPosition++;
            else
                buffer |= ((value) ? 1 : 0) << (BlockSize - bitPosition++ - 1);
        }

        public void WriteBits(long value, int bitCount)
        {
            if (bitCount > 0)
            {
                if (EffectiveBitOrder == BitOrder.LSBFirst)
                {
                    for (int i = 0; i < bitCount; i++)
                    {
                        WriteBit((value & 1) == 1);
                        value >>= 1;
                    }
                }
                else
                {
                    for (int i = bitCount - 1; i >= 0; i--)
                    {
                        WriteBit(((value >> i) & 1) == 1);
                    }
                }
            }
            else
            {
                throw new Exception("BitCount needs to be greater than 0");
            }
        }

        public void WriteStruct<T>(T obj)
        {
            WriteObject(obj);
        }

        public void Write<T>(IEnumerable<T> list)
        {
            foreach (var element in list)
                WriteStruct(element);
        }
        #endregion
    }
}