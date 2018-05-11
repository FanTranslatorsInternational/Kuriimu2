using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Komponent.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _nibble = -1;

        public ByteOrder ByteOrder { get; set; }

        public BinaryWriterX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode)
        {
            ByteOrder = byteOrder;
        }

        public BinaryWriterX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, encoding)
        {
            ByteOrder = byteOrder;
        }

        public BinaryWriterX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
        }

        // Parameters out of order with a default encoding of Unicode
        public BinaryWriterX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode, leaveOpen)
        {
            ByteOrder = byteOrder;
        }

        public void WriteStruct<T>(T item) => Write(item.StructToBytes(ByteOrder));

        public override void Write(short value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(int value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(long value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ushort value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(uint value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ulong value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        // Custom Methods
        public void WriteASCII(string value)
        {
            base.Write(Encoding.ASCII.GetBytes(value));
        }

        public void WriteString(Encoding encoding, string value)
        {
            var bytes = encoding.GetBytes(value);
            Write((byte)bytes.Length);
            Write(bytes);
        }

        public void WriteNibble(int val)
        {
            val &= 15;
            if (_nibble == -1)
                _nibble = val;
            else
            {
                Write((byte)(_nibble + 16 * val));
                _nibble = -1;
            }
        }

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
    }
}