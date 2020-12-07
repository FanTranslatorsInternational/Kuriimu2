using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO.BinarySupport;
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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

            return ByteOrder == ByteOrder.LittleEndian ? base.ReadDecimal() : ReadBytes(16).Reverse().ToArray().ToDecimal();
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

        #endregion

        #region Peeks

        // String
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

        // Byte
        public byte PeekByte(long offset)
        {
            var startOffset = BaseStream.Position;

            BaseStream.Position = offset;
            var value = ReadByte();

            BaseStream.Position = startOffset;

            return value;
        }

        public byte[] PeekBytes(int length = 1, long offset = 0)
        {
            var startOffset = BaseStream.Position;

            BaseStream.Position = offset;
            var value = ReadBytes(length);

            BaseStream.Position = startOffset;

            return value;
        }

        public ushort PeekUInt16()
        {
            var startOffset = BaseStream.Position;

            var value = ReadUInt16();

            BaseStream.Position = startOffset;

            return value;
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

        public object ReadType(Type type)
        {
            var typeReader = new TypeReader();
            return typeReader.ReadType(this, type);
        }

        public IList<T> ReadMultiple<T>(int count) => ReadMultipleInternal(count, typeof(T)).Cast<T>().ToArray();

        public IList<object> ReadMultiple(int count, Type type) => ReadMultipleInternal(count, type).ToArray();

        private IEnumerable<object> ReadMultipleInternal(int count, Type type) =>
            Enumerable.Range(0, count).Select(_ => ReadType(type));

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

        public byte[] ReadBytesUntil(params byte[] stop)
        {
            var result = new List<byte>();

            byte b = ReadByte();
            while (stop.All(s => s != b) && BaseStream.Position < BaseStream.Length)
            {
                result.Add(b);
                b = ReadByte();
            }

            return result.ToArray();
        }

        // Bit Fields
        public bool ReadBit()
        {
            return ReadBitInteger() == 1;
        }

        private int ReadBitInteger()
        {
            ResetNibbleBuffer();

            if (_bitPosition >= _currentBlockSize * 8)
                FillBuffer();

            if (EffectiveBitOrder == BitOrder.LeastSignificantBitFirst)
            {
                return (int)((_buffer >> _bitPosition++) & 0x1);
            }

            return (int)((_buffer >> (_currentBlockSize * 8 - _bitPosition++ - 1)) & 0x1);
        }

        public object ReadBits(int count)
        {
            // Same design pattern as BitReader
            /*
            * This method is designed with direct mapping in mind.
            *
            * Example:
            * You have a byte 0x83, which in bits would be
            * 0b1000 0011
            *
            * Assume we read 3 bits and 5 bits afterwards
            *
            * Assuming MsbFirst, we would now read the values
            * 0b100 and 0b00011
            *
            * Assuming LsbFirst, we would now read the values
            * 0b011 and 0b10000
            *
            * Even though the values themselves changed, the order of bits is still intact
            *
            * Combine 0b100 and 0b00011 and you get the original byte
            * Combine 0b10000 and 0b011 and you also get the original byte
            *
            */

            long result = 0;
            for (var i = 0; i < count; i++)
            {
                if (EffectiveBitOrder == BitOrder.MostSignificantBitFirst)
                {
                    result <<= 1;
                    result |= (byte)ReadBitInteger();
                }
                else
                {
                    result |= (long)(ReadBitInteger() << i);
                }
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