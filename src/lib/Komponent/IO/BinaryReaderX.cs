using System.Buffers.Binary;
using System.Text;
using Komponent.Contract.Enums;
using Komponent.Contract.Exceptions;

namespace Komponent.IO
{
    public sealed class BinaryReaderX : BinaryReader
    {
        private int _blockSize;
        private int _currentBlockSize;
        private int _bitPosition = 64;
        private long _buffer;

        private readonly int _encodingNullLength;
        private readonly Encoding _encoding;

        /// <summary>
        /// Gets or sets the order in which to read bytes.
        /// </summary>
        public ByteOrder ByteOrder { get; set; }

        /// <summary>
        /// Gets or sets the order in which to read bits.
        /// </summary>
        public BitOrder BitOrder { get; set; }

        /// <summary>
        /// Gets or sets the size of a bit block in bytes.
        /// </summary>
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

        public BinaryReaderX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, Encoding.UTF8, true, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryReaderX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, Encoding.UTF8, leaveOpen, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryReaderX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, encoding, true, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryReaderX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;

            _encoding = encoding;
            _encodingNullLength = _encoding.GetByteCount("\0");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #endregion

        #region Value Reads

        public override int Read()
        {
            Reset();

            return base.Read();
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            Reset();

            return base.Read(buffer, index, count);
        }

        public override int Read(Span<byte> buffer)
        {
            Reset();

            return base.Read(buffer);
        }

        public override int Read(Span<char> buffer)
        {
            Reset();

            return base.Read(buffer);
        }

        public override byte[] ReadBytes(int count)
        {
            Reset();

            return base.ReadBytes(count);
        }

        public override bool ReadBoolean()
        {
            Reset();

            return base.ReadBoolean();
        }

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

        public override int Read(char[] buffer, int index, int count)
        {
            Reset();

            return base.Read(buffer, index, count);
        }

        public override short ReadInt16()
        {
            Reset();

            byte[] buffer = ReadBytes(2);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadInt16LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadInt16BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override Half ReadHalf()
        {
            Reset();

            return base.ReadHalf();
        }

        public override int ReadInt32()
        {
            Reset();

            byte[] buffer = ReadBytes(4);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadInt32LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadInt32BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override long ReadInt64()
        {
            Reset();

            byte[] buffer = ReadBytes(8);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadInt64LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadInt64BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override ushort ReadUInt16()
        {
            Reset();

            byte[] buffer = ReadBytes(2);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt16LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt16BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override uint ReadUInt32()
        {
            Reset();

            byte[] buffer = ReadBytes(4);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt32LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt32BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override ulong ReadUInt64()
        {
            Reset();

            byte[] buffer = ReadBytes(8);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt64LittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt64BigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override float ReadSingle()
        {
            Reset();

            byte[] buffer = ReadBytes(4);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadSingleLittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadSingleBigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override double ReadDouble()
        {
            Reset();

            byte[] buffer = ReadBytes(8);
            return ByteOrder switch
            {
                ByteOrder.LittleEndian => BinaryPrimitives.ReadDoubleLittleEndian(buffer),
                ByteOrder.BigEndian => BinaryPrimitives.ReadDoubleBigEndian(buffer),
                _ => throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.")
            };
        }

        public override decimal ReadDecimal()
        {
            int lo, mid, hi, flags;

            Reset();

            byte[] buffer = ReadBytes(16);
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    lo = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                    mid = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
                    hi = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8));
                    flags = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12));
                    return new decimal([lo, mid, hi, flags]);

                case ByteOrder.BigEndian:
                    flags = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    hi = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(4));
                    mid = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(8));
                    lo = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(12));
                    return new decimal([lo, mid, hi, flags]);

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }
        }

        #endregion

        #region String Reads

        public override string ReadString()
        {
            Reset();

            return base.ReadString();
        }

        public string ReadNullTerminatedString()
        {
            Reset();

            var result = new List<byte>(0x400);

            var buffer = new byte[_encodingNullLength];
            while (BaseStream.Position < BaseStream.Length)
            {
                var shouldStop = true;

                int length = BaseStream.Read(buffer);
                if (length >= _encodingNullLength)
                {
                    for (var i = 0; i < _encodingNullLength; i++)
                        shouldStop &= buffer[i] == 0;
                }

                if (shouldStop)
                    break;

                result.AddRange(buffer);
            }

            return _encoding.GetString([.. result]);
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

        #region Value Peeks

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

        #endregion

        #region String Peeks

        public string PeekString(int length)
        {
            return PeekString(0, length, _encoding);
        }

        public string PeekString(long offset, int length)
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

        #region Bit Reads

        public int ReadBit()
        {
            if (_bitPosition >= _currentBlockSize * 8)
                FillBitBuffer();

            return BitOrder switch
            {
                BitOrder.LeastSignificantBitFirst => (int)((_buffer >> _bitPosition++) & 0x1),
                BitOrder.MostSignificantBitFirst => (int)((_buffer >> (_currentBlockSize * 8 - _bitPosition++ - 1)) &
                                                          0x1),
                _ => throw new InvalidOperationException($"Unsupported bit order {BitOrder}.")
            };
        }

        public T ReadBits<T>(int count)
        {
            if (typeof(T) != typeof(bool) &&
                typeof(T) != typeof(sbyte) && typeof(T) != typeof(byte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong))
                throw new UnsupportedTypeException(typeof(T));

            object value = ReadBits(count);

            return (T)Convert.ChangeType(value, typeof(T));
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
                switch (BitOrder)
                {
                    case BitOrder.LeastSignificantBitFirst:
                        result |= (long)ReadBit() << i;
                        break;

                    case BitOrder.MostSignificantBitFirst:
                        result <<= 1;
                        result |= (byte)ReadBit();
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported bit order {BitOrder}.");
                }
            }

            return result;
        }

        private void Reset()
        {
            ResetBitBuffer();
        }

        internal void ResetBitBuffer()
        {
            _bitPosition = 64;
            _buffer = 0;
        }

        private void FillBitBuffer()
        {
            _currentBlockSize = _blockSize;

            _buffer = _blockSize switch
            {
                1 => ReadByte(),
                2 => ReadInt16(),
                4 => ReadInt32(),
                8 => ReadInt64(),
                _ => _buffer
            };

            _bitPosition = 0;
        }

        #endregion

        protected override void FillBuffer(int numBytes)
        {
            Reset();

            base.FillBuffer(numBytes);
        }
    }
}