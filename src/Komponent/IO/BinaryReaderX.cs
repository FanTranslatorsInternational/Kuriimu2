using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Komponent.IO
{
    public class BinaryReaderX : BinaryReader
    {
        private int _nibble = -1;

        public ByteOrder ByteOrder { get; set; }

        public BinaryReaderX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode)
        {
            ByteOrder = byteOrder;
        }

        public BinaryReaderX(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, encoding)
        {
            ByteOrder = byteOrder;
        }

        public BinaryReaderX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, encoding, leaveOpen)
        {
            ByteOrder = byteOrder;
        }

        // Parameters out of order with a default encoding of Unicode
        public BinaryReaderX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode, leaveOpen)
        {
            ByteOrder = byteOrder;
        }

        public string ReadCStringA() => string.Concat(Enumerable.Range(0, 999).Select(_ => (char)ReadByte()).TakeWhile(c => c != 0));
        public string ReadCStringW() => string.Concat(Enumerable.Range(0, 999).Select(_ => (char)ReadInt16()).TakeWhile(c => c != 0));
        public string ReadCStringSJIS() => Encoding.GetEncoding("Shift-JIS").GetString(Enumerable.Range(0, 999).Select(_ => ReadByte()).TakeWhile(c => c != 0).ToArray());
        public T ReadStruct<T>() => ReadBytes(Marshal.SizeOf<T>()).BytesToStruct<T>(ByteOrder);
        public List<T> ReadMultiple<T>(int count, Func<int, T> func) => Enumerable.Range(0, count).Select(func).ToList();
        public List<T> ReadMultiple<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();

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

        public override short ReadInt16()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt16();
            return BitConverter.ToInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override int ReadInt32()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt32();
            return BitConverter.ToInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override long ReadInt64()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadInt64();
            return BitConverter.ToInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override ushort ReadUInt16()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt16();
            return BitConverter.ToUInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override uint ReadUInt32()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt32();
            return BitConverter.ToUInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override ulong ReadUInt64()
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                return base.ReadUInt64();
            return BitConverter.ToUInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        // Custom Methods
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

        public byte PeekByte()
        {
            var value = ReadByte();
            BaseStream.Position--;
            return value;
        }

        public int PeekInt32()
        {
            var value = ReadInt32();
            BaseStream.Position -= 4;
            return value;
        }

        public int ReadNibble()
        {
            if (_nibble == -1)
            {
                _nibble = ReadByte();
                return _nibble % 16;
            }
            var val = _nibble / 16;
            _nibble = -1;
            return val;
        }

        public byte SeekAlignment(int alignment = 16, byte alignmentByte = 0x0)
        {
            var remainder = BaseStream.Position % alignment;
            if (remainder <= 0) return alignmentByte;
            alignmentByte = ReadByte();
            BaseStream.Position += alignment - remainder - 1;

            return alignmentByte;
        }

        public byte SeekAlignment(byte alignmentByte, int alignment = 16) => SeekAlignment(alignment, alignmentByte);
    }
}