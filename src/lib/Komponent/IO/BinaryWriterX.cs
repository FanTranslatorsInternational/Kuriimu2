using System.Buffers.Binary;
using System.Text;
using Komponent.Contract.Enums;

namespace Komponent.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _blockSize;
        private int _bitPosition;
        private long _buffer;

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
            get => _blockSize;
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 8)
                    throw new InvalidOperationException("BlockSize can only be 1, 2, 4, or 8.");

                _blockSize = value;
            }
        }

        #region Constructors

        public BinaryWriterX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, Encoding.UTF8, true, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryWriterX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, Encoding.UTF8, leaveOpen, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryWriterX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : this(input, encoding, true, byteOrder, bitOrder, blockSize)
        {
        }

        public BinaryWriterX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
            : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
            BitOrder = bitOrder;
            BlockSize = blockSize;

            _encoding = encoding;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #endregion

        public override void Flush()
        {
            FlushBitBuffer();

            base.Flush();
        }

        #region Value Writes

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

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Flush();

            base.Write(buffer);
        }

        public override void Write(char[] chars, int index, int count)
        {
            Flush();

            base.Write(chars, index, count);
        }

        public override void Write(ReadOnlySpan<char> chars)
        {
            Flush();

            base.Write(chars);
        }

        public override void Write(bool value)
        {
            Flush();

            base.Write(value);
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

        public override void Write(short value)
        {
            Flush();

            var buffer = new byte[2];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteInt16BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(int value)
        {
            Flush();

            var buffer = new byte[4];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteInt32BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(Half value)
        {
            Flush();

            base.Write(value);
        }

        public override void Write(long value)
        {
            Flush();

            var buffer = new byte[8];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteInt64BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(ushort value)
        {
            Flush();

            var buffer = new byte[2];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(uint value)
        {
            Flush();

            var buffer = new byte[4];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(ulong value)
        {
            Flush();

            var buffer = new byte[8];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(float value)
        {
            Flush();

            var buffer = new byte[4];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteSingleBigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(double value)
        {
            Flush();

            var buffer = new byte[8];
            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        public override void Write(decimal value)
        {
            Flush();

            var buffer = new byte[16];
            int[] bits = decimal.GetBits(value);

            switch (ByteOrder)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteInt32LittleEndian(buffer, bits[0]);
                    BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), bits[1]);
                    BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(8), bits[2]);
                    BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(12), bits[3]);
                    break;

                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteInt32BigEndian(buffer, bits[3]);
                    BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(4), bits[2]);
                    BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(8), bits[1]);
                    BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(12), bits[0]);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported byte order {ByteOrder}.");
            }

            Write(buffer);
        }

        #endregion

        #region String Writes

        public override void Write(string value)
        {
            Flush();

            base.Write(value);
        }

        public void WriteString(string value, bool writeLeadingCount = false, bool writeNullTerminator = true)
        {
            WriteString(value, _encoding, writeLeadingCount, writeNullTerminator);
        }

        public void WriteString(string value, Encoding encoding, bool writeLeadingCount = false, bool writeNullTerminator = true)
        {
            if (writeNullTerminator)
                value += "\0";

            byte[] bytes = encoding.GetBytes(value);

            if (writeLeadingCount)
                Write((byte)bytes.Length);

            Write(bytes);
        }

        #endregion

        #region Alignement/Padding Writes

        public void WritePadding(int count, byte paddingByte = 0)
        {
            for (var i = 0; i < count; i++)
                Write(paddingByte);
        }

        public void WriteAlignment(int alignment, byte alignmentByte = 0)
        {
            long remainder = BaseStream.Position % alignment;
            if (remainder <= 0)
                return;

            for (var i = 0; i < alignment - remainder; i++)
                Write(alignmentByte);
        }

        #endregion

        #region Bit Writes

        public void WriteBit(long value)
        {
            _buffer |= BitOrder switch
            {
                BitOrder.LeastSignificantBitFirst => (value & 1L) << _bitPosition++,
                BitOrder.MostSignificantBitFirst => (value & 1L) << (BlockSize * 8 - _bitPosition++ - 1),
                _ => throw new InvalidOperationException($"Unsupported bit order {BitOrder}.")
            };

            if (_bitPosition >= BlockSize * 8)
                Flush();
        }

        private void WriteBit(long value, bool writeBuffer)
        {
            _buffer |= BitOrder switch
            {
                BitOrder.LeastSignificantBitFirst => (value & 1L) << _bitPosition++,
                BitOrder.MostSignificantBitFirst => (value & 1L) << (BlockSize * 8 - _bitPosition++ - 1),
                _ => throw new InvalidOperationException($"Unsupported bit order {BitOrder}.")
            };

            if (writeBuffer)
                Flush();
        }

        public void WriteBits(long value, int bitCount)
        {
            if (bitCount > 0)
            {
                switch (BitOrder)
                {
                    case BitOrder.LeastSignificantBitFirst:
                        for (var i = 0; i < bitCount; i++)
                        {
                            WriteBit(value, _bitPosition + 1 >= BlockSize * 8);
                            value >>= 1;
                        }
                        break;

                    case BitOrder.MostSignificantBitFirst:
                        for (var i = bitCount - 1; i >= 0; i--)
                        {
                            WriteBit((value >> i), _bitPosition + 1 >= BlockSize * 8);
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported bit order {BitOrder}.");
                }
            }
            else
            {
                throw new Exception("BitCount needs to be greater than 0.");
            }
        }

        private void FlushBitBuffer()
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

        #endregion
    }
}