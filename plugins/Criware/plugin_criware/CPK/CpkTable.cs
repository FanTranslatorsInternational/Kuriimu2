using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Format class that handles CPK Tables.
    /// </summary>
    public class CpkTable
    {
        /// <summary>
        /// A stream of the entire table (minus the header).
        /// </summary>
        private readonly BinaryReaderX _tableStream = null;

        /// <summary>
        /// A stream of the string data for the table.
        /// </summary>
        private readonly BinaryReaderX _stringStream = null;

        /// <summary>
        /// A stream of the binary data for the table.
        /// </summary>
        private readonly BinaryReaderX _binaryStream = null;

        /// <summary>
        /// The table header.
        /// </summary>
        public CpkTableHeader Header { get; }

        /// <summary>
        /// The table info.
        /// </summary>
        public CpkTableInfo TableInfo { get; }

        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The list of columns in the table.
        /// </summary>
        public List<CpkColumnInfo> Columns { get; }

        /// <summary>
        /// The list of rows in the table.
        /// </summary>
        public List<CpkRow> Rows { get; }

        #region Properties

        /// <summary>
        /// Retrieves all of the column names.
        /// </summary>
        public List<string> ColumnNames => Columns?.Select(c => c.Name).ToList();

        #endregion

        /// <summary>
        /// Instantiates a new <see cref="CpkTable"/> from an input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        public CpkTable(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read in the table header.
                Header = br.ReadStruct<CpkTableHeader>();

                // Create a sub stream of the entire table.
                _tableStream = new BinaryReaderX(new SubStream(br.BaseStream, br.BaseStream.Position, Header.TableSize), true, ByteOrder.BigEndian);

                // Make sure the data isn't bogus.
                if (Header.PacketSize - 8 != Header.TableSize)
                    throw new FormatException("The packet size doesn't match the table size. The file might be corrupt or encrypted or not a CPK.");
                if (Header.PacketSize > 100 * 1024 * 1024)
                    throw new FormatException("The packet size is too big. The file might be corrupt or encrypted or not a CPK.");

                // Switch to Big Endian to read the table info.
                br.ByteOrder = ByteOrder.BigEndian;
                TableInfo = _tableStream.ReadStruct<CpkTableInfo>();

                // Create sub streams for the string data and binary data.
                _stringStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.StringsOffset, Header.TableSize - TableInfo.StringsOffset), true, ByteOrder.BigEndian);
                _binaryStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.BinaryOffset, Header.TableSize - TableInfo.BinaryOffset), true, ByteOrder.BigEndian);

                // Read in the table name.
                Name = ReadString(TableInfo.NameOffset);

                // Read in the columns.
                var columns = new List<CpkColumnInfo>();
                for (var i = 0; i < TableInfo.ColumnCount; i++)
                {
                    var flags = _tableStream.ReadByte();
                    var col = new CpkColumnInfo
                    {
                        Name = ReadString(_tableStream.ReadInt32()),
                        Storage = (CpkColumnStorage)(flags & 0xF0),
                        Type = (CpkDataType)(flags & 0x0F)
                    };
                    columns.Add(col);

                    if (col.Storage == CpkColumnStorage.Const)
                        col.Value = ReadValue(col.Type);
                    if (col.Storage == CpkColumnStorage.Zero)
                        col.Value = ZeroValue(col.Type);
                }
                Columns = columns;

                // Read in the values.
                _tableStream.BaseStream.Position = TableInfo.ValuesOffset;
                Rows = new List<CpkRow>();
                for (var i = 0; i < TableInfo.RowCount; i++)
                {
                    var row = new CpkRow(Columns);

                    foreach (var col in Columns)
                    {
                        if (col.Storage == CpkColumnStorage.Const || col.Storage == CpkColumnStorage.Zero)
                            row[col.Name].Value = col.Value.Value;
                        else if (col.Storage == CpkColumnStorage.Row)
                            row[col.Name].Value = ReadValue(col.Type).Value;
                    }

                    Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// Reads a string from the given offset.
        /// </summary>
        /// <param name="offset">Offset to the string.</param>
        /// <returns>A string.</returns>
        private string ReadString(int offset)
        {
            _stringStream.BaseStream.Position = offset;
            return _stringStream.ReadString();
        }

        /// <summary>
        /// Reads 'length' bytes from the given offset.
        /// </summary>
        /// <param name="offset">Offset to the data.</param>
        /// <param name="length">Length to read.</param>
        /// <returns>Some bytes.</returns>
        private byte[] ReadData(int offset, int length)
        {
            _binaryStream.BaseStream.Position = offset;
            return _binaryStream.ReadBytes(length);
        }

        /// <summary>
        /// Reads a value based on the given <see cref="CpkDataType"/>.
        /// </summary>
        /// <param name="type">The type of value to read.</param>
        /// <returns>A <see cref="CpkValue"/> containing the type and read value.</returns>
        private CpkValue ReadValue(CpkDataType type)
        {
            switch (type)
            {
                case CpkDataType.UInt8:
                    return new CpkValue { Type = type, Value = _tableStream.ReadByte() };
                case CpkDataType.SInt8:
                    return new CpkValue { Type = type, Value = _tableStream.ReadSByte() };
                case CpkDataType.UInt16:
                    return new CpkValue { Type = type, Value = _tableStream.ReadUInt16() };
                case CpkDataType.SInt16:
                    return new CpkValue { Type = type, Value = _tableStream.ReadInt16() };
                case CpkDataType.UInt32:
                    return new CpkValue { Type = type, Value = _tableStream.ReadUInt32() };
                case CpkDataType.SInt32:
                    return new CpkValue { Type = type, Value = _tableStream.ReadInt32() };
                case CpkDataType.UInt64:
                    return new CpkValue { Type = type, Value = _tableStream.ReadUInt64() };
                case CpkDataType.SInt64:
                    return new CpkValue { Type = type, Value = _tableStream.ReadInt64() };
                case CpkDataType.Float:
                    return new CpkValue { Type = type, Value = _tableStream.ReadSingle() };
                case CpkDataType.String:
                    return new CpkValue { Type = type, Value = ReadString(_tableStream.ReadInt32()) };
                case CpkDataType.Data:
                    return new CpkValue { Type = type, Value = ReadData(_tableStream.ReadInt32(), _tableStream.ReadInt32()) };
                default:
                    return new CpkValue();
            }
        }

        /// <summary>
        /// Returns a zero or null value based on the given <see cref="CpkDataType"/>.
        /// </summary>
        /// <param name="type">The type of zero value to return.</param>
        /// <returns>A <see cref="CpkValue"/> containing the type and zero value.</returns>
        private CpkValue ZeroValue(CpkDataType type)
        {
            switch (type)
            {
                case CpkDataType.UInt8:
                    return new CpkValue { Type = type, Value = (byte)0 };
                case CpkDataType.SInt8:
                    return new CpkValue { Type = type, Value = (sbyte)0 };
                case CpkDataType.UInt16:
                    return new CpkValue { Type = type, Value = (ushort)0 };
                case CpkDataType.SInt16:
                    return new CpkValue { Type = type, Value = (short)0 };
                case CpkDataType.UInt32:
                    return new CpkValue { Type = type, Value = (uint)0 };
                case CpkDataType.SInt32:
                    return new CpkValue { Type = type, Value = (int)0 };
                case CpkDataType.UInt64:
                    return new CpkValue { Type = type, Value = (ulong)0 };
                case CpkDataType.SInt64:
                    return new CpkValue { Type = type, Value = (long)0 };
                case CpkDataType.Float:
                    return new CpkValue { Type = type, Value = (float)0 };
                case CpkDataType.String:
                    return new CpkValue { Type = type, Value = null };
                case CpkDataType.Data:
                    return new CpkValue { Type = type, Value = null };
                default:
                    return new CpkValue();
            }
        }
    }
}
