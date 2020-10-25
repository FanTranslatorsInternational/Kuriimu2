using System;
using Komponent.IO;

namespace plugin_criware.Archives.Support
{
    /// <summary>
    /// CPK value storage class.
    /// </summary>
    public class CpkValue
    {
        private object _value;

        /// <summary>
        /// The data type of the value.
        /// </summary>
        public CpkDataType Type { get; }

        /// <summary>
        /// Instantiates a new <see cref="CpkValue"/>.
        /// </summary>
        /// <param name="type">The data type of the value.</param>
        public CpkValue(CpkDataType type)
        {
            Type = type;
        }

        public TType Get<TType>()
        {
            return ConvertValue<TType>(_value);
        }

        public void Set(object value)
        {
            switch (Type)
            {
                case CpkDataType.UInt8:
                    _value = ConvertValue<byte>(value);
                    break;

                case CpkDataType.SInt8:
                    _value = ConvertValue<sbyte>(value);
                    break;

                case CpkDataType.UInt16:
                    _value = ConvertValue<ushort>(value);
                    break;

                case CpkDataType.SInt16:
                    _value = ConvertValue<short>(value);
                    break;

                case CpkDataType.UInt32:
                    _value = ConvertValue<uint>(value);
                    break;

                case CpkDataType.SInt32:
                    _value = ConvertValue<int>(value);
                    break;

                case CpkDataType.UInt64:
                    _value = ConvertValue<ulong>(value);
                    break;

                case CpkDataType.SInt64:
                    _value = ConvertValue<long>(value);
                    break;

                case CpkDataType.Float:
                    _value = ConvertValue<float>(value);
                    break;

                case CpkDataType.String:
                    _value = ConvertValue<string>(value);
                    break;

                case CpkDataType.Data:
                    _value = ConvertValue<byte[]>(value);
                    break;

                default:
                    throw new InvalidOperationException($"'{Type}' not supported.");
            }
        }

        public int GetSize()
        {
            switch (Type)
            {
                case CpkDataType.UInt8:
                    return 1;

                case CpkDataType.SInt8:
                    return 1;

                case CpkDataType.UInt16:
                    return 2;

                case CpkDataType.SInt16:
                    return 2;

                case CpkDataType.UInt32:
                    return 4;

                case CpkDataType.SInt32:
                    return 4;

                case CpkDataType.UInt64:
                    return 8;

                case CpkDataType.SInt64:
                    return 8;

                case CpkDataType.Float:
                    return 4;

                case CpkDataType.String:
                    return 4;

                case CpkDataType.Data:
                    return 8;

                default:
                    throw new InvalidOperationException($"'{Type}' not supported.");
            }
        }

        public bool IsDefault()
        {
            switch (Type)
            {
                case CpkDataType.UInt8:
                    return (byte)_value == 0;

                case CpkDataType.SInt8:
                    return (sbyte)_value == 0;

                case CpkDataType.UInt16:
                    return (ushort)_value == 0;

                case CpkDataType.SInt16:
                    return (short)_value == 0;

                case CpkDataType.UInt32:
                    return (uint)_value == 0;

                case CpkDataType.SInt32:
                    return (int)_value == 0;

                case CpkDataType.UInt64:
                    return (ulong)_value == 0;

                case CpkDataType.SInt64:
                    return (long)_value == 0;

                case CpkDataType.Float:
                    return (float)_value == 0;

                case CpkDataType.String:
                    return (string)_value == null;

                case CpkDataType.Data:
                    return (byte[])_value == null;

                default:
                    throw new InvalidOperationException($"'{Type}' not supported.");
            }
        }

        public void Write(BinaryWriterX bw, StringWriter writer, ref long tablePosition, ref long dataPosition, long dataOffset)
        {
            bw.BaseStream.Position = tablePosition;

            switch (Type)
            {
                case CpkDataType.UInt8:
                    bw.Write((byte)_value);
                    break;

                case CpkDataType.SInt8:
                    bw.Write((sbyte)_value);
                    break;

                case CpkDataType.UInt16:
                    bw.Write((ushort)_value);
                    break;

                case CpkDataType.SInt16:
                    bw.Write((short)_value);
                    break;

                case CpkDataType.UInt32:
                    bw.Write((uint)_value);
                    break;

                case CpkDataType.SInt32:
                    bw.Write((int)_value);
                    break;

                case CpkDataType.UInt64:
                    bw.Write((ulong)_value);
                    break;

                case CpkDataType.SInt64:
                    bw.Write((long)_value);
                    break;

                case CpkDataType.Float:
                    bw.Write((float)_value);
                    break;

                case CpkDataType.String:
                    bw.Write((int)writer.WriteString((string)_value));
                    break;

                case CpkDataType.Data:
                    bw.Write((int)(dataPosition - dataOffset));
                    bw.Write(((byte[])_value).Length);
                    CpkSupport.WriteData(bw, dataPosition, (byte[])_value);

                    dataPosition = bw.BaseStream.Position;
                    break;

                default:
                    throw new InvalidOperationException($"'{Type}' not supported.");
            }

            tablePosition += GetSize();
        }

        public static CpkValue Read(CpkDataType type, BinaryReaderX br, BinaryReaderX stringBr, BinaryReaderX dataBr)
        {
            switch (type)
            {
                case CpkDataType.UInt8:
                    return new CpkValue(type) { _value = br.ReadByte() };

                case CpkDataType.SInt8:
                    return new CpkValue(type) { _value = br.ReadSByte() };

                case CpkDataType.UInt16:
                    return new CpkValue(type) { _value = br.ReadUInt16() };

                case CpkDataType.SInt16:
                    return new CpkValue(type) { _value = br.ReadInt16() };

                case CpkDataType.UInt32:
                    return new CpkValue(type) { _value = br.ReadUInt32() };

                case CpkDataType.SInt32:
                    return new CpkValue(type) { _value = br.ReadInt32() };

                case CpkDataType.UInt64:
                    return new CpkValue(type) { _value = br.ReadUInt64() };

                case CpkDataType.SInt64:
                    return new CpkValue(type) { _value = br.ReadInt64() };

                case CpkDataType.Float:
                    return new CpkValue(type) { _value = br.ReadSingle() };

                case CpkDataType.String:
                    return new CpkValue(type) { _value = CpkSupport.ReadString(stringBr, br.ReadInt32()) };

                case CpkDataType.Data:
                    return new CpkValue(type) { _value = CpkSupport.ReadBytes(dataBr, br.ReadInt32(), br.ReadInt32()) };

                default:
                    throw new InvalidOperationException($"'{type}' not supported.");
            }
        }

        public static CpkValue Default(CpkDataType type)
        {
            switch (type)
            {
                case CpkDataType.UInt8:
                    return new CpkValue(type) { _value = (byte)0 };

                case CpkDataType.SInt8:
                    return new CpkValue(type) { _value = (sbyte)0 };

                case CpkDataType.UInt16:
                    return new CpkValue(type) { _value = (ushort)0 };

                case CpkDataType.SInt16:
                    return new CpkValue(type) { _value = (short)0 };

                case CpkDataType.UInt32:
                    return new CpkValue(type) { _value = (uint)0 };

                case CpkDataType.SInt32:
                    return new CpkValue(type) { _value = (int)0 };

                case CpkDataType.UInt64:
                    return new CpkValue(type) { _value = (ulong)0 };

                case CpkDataType.SInt64:
                    return new CpkValue(type) { _value = (long)0 };

                case CpkDataType.Float:
                    return new CpkValue(type) { _value = (float)0 };

                case CpkDataType.String:
                    return new CpkValue(type) { _value = null };

                case CpkDataType.Data:
                    return new CpkValue(type) { _value = null };

                default:
                    throw new InvalidOperationException($"'{type}' not supported.");
            }
        }

        private TType ConvertValue<TType>(object value)
        {
            return (TType)Convert.ChangeType(value, typeof(TType));
        }
    }
}
