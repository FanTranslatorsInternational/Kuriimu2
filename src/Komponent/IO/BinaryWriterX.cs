using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Komponent.Extensions;
using Komponent.IO.BinarySupport;
using Kontract.Models.IO;

namespace Komponent.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _nibble = -1;
        private int _blockSize;
        private int _bitPosition = 0;
        private long _buffer;

        public ByteOrder ByteOrder { get; set; }
        public NibbleOrder NibbleOrder { get; set; }
        public BitOrder BitOrder { get; set; }

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
            get => _blockSize;
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 8)
                    throw new InvalidOperationException("BlockSize can only be 1, 2, 4, or 8.");

                _blockSize = value;
            }
        }

        #region Constructors

        public BinaryWriterX(Stream input,
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

        public BinaryWriterX(Stream input,
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

        public BinaryWriterX(Stream input,
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

        public BinaryWriterX(Stream input,
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
                base.Write(value.GetBytes().Reverse().ToArray());
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

        public void WriteMultiple<T>(IEnumerable<T> list)
        {
            foreach (var element in list)
                WriteType(element);
        }

        public void WriteType(object value)
        {
            var typeWriter = new TypeWriter();
            typeWriter.WriteType(this, value);
        }

        #endregion

        #region Custom Methods

        public void WriteNibble(int val)
        {
            FlushBuffer();

            val &= 15;
            if (_nibble == -1)
                _nibble = NibbleOrder == NibbleOrder.LowNibbleFirst ? val : val * 16;
            else
            {
                _nibble += NibbleOrder == NibbleOrder.LowNibbleFirst ? val * 16 : val;
                FlushNibble();
            }
        }

        // Bit Fields
        public void WriteBit(bool value)
        {
            FlushNibble();

            if (EffectiveBitOrder == BitOrder.LeastSignificantBitFirst)
                _buffer |= ((value) ? 1 : 0) << _bitPosition++;
            else
                _buffer |= ((value) ? 1 : 0) << (BlockSize * 8 - _bitPosition++ - 1);

            if (_bitPosition >= BlockSize * 8)
                Flush();
        }

        private void WriteBit(bool value, bool writeBuffer)
        {
            FlushNibble();

            if (EffectiveBitOrder == BitOrder.LeastSignificantBitFirst)
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
                if (EffectiveBitOrder == BitOrder.LeastSignificantBitFirst)
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