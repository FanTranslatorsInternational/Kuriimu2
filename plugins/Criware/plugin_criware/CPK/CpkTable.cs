using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Format class that handles CPK Tables.
    /// </summary>
    public class CpkTable
    {
        /// <summary>
        /// A stream of the entire table (minus the table header).
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

        /// <summary>
        /// Gets or Sets a value dictating if the UTF tables are obfuscated.
        /// </summary>
        public bool UtfObfuscation { get; set; }

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
                br.ByteOrder = ByteOrder.BigEndian;

                // Handle obfuscated UTF table
                if (br.PeekString() != "@UTF")
                {
                    UtfObfuscation = true;

                    // Decrypt the UTF table
                    _tableStream = new BinaryReaderX(new MemoryStream(UtfTools.XorUtf(br.ReadBytes(Header.PacketSize))), true, ByteOrder.BigEndian);

                    // Read the table info.
                    TableInfo = _tableStream.ReadStruct<CpkTableInfo>();

                    // Replace the stream with a new SubStream into the decrypted data.
                    _tableStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, 0x8, TableInfo.TableSize), true, ByteOrder.BigEndian);
                }
                else
                {
                    // Read the table info.
                    TableInfo = br.ReadStruct<CpkTableInfo>();

                    // Create a sub stream of the entire table.
                    _tableStream = new BinaryReaderX(new SubStream(br.BaseStream, 0x18, TableInfo.TableSize), true, ByteOrder.BigEndian);
                }

                // Move forward to right after TableInfo
                _tableStream.BaseStream.Position += 0x18;

                // Make sure the data isn't bogus.
                if (Header.PacketSize - 0x8 != TableInfo.TableSize)
                    throw new FormatException("The packet size doesn't match the table size. The file might be corrupt, encrypted or it is not a CPK.");
                if (Header.PacketSize > 100 * 1024 * 1024)
                    throw new FormatException("The packet size is too big. The file might be corrupt, encrypted or it is not a CPK.");

                // Create sub streams for the string data and binary data.
                _stringStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.StringsOffset, TableInfo.TableSize - TableInfo.StringsOffset), true, ByteOrder.BigEndian);
                _binaryStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.BinaryOffset, TableInfo.TableSize - TableInfo.BinaryOffset), true, ByteOrder.BigEndian);

                // Read in the table name.
                Name = ReadString(TableInfo.NameOffset);

                // Read in the columns.
                var columns = new List<CpkColumnInfo>();
                for (var i = 0; i < TableInfo.ColumnCount; i++)
                {
                    var flags = _tableStream.ReadByte();
                    var column = new CpkColumnInfo
                    {
                        Name = ReadString(_tableStream.ReadInt32()),
                        Storage = (CpkColumnStorage)(flags & 0xF0),
                        Type = (CpkDataType)(flags & 0x0F)
                    };
                    columns.Add(column);

                    if (column.Storage == CpkColumnStorage.Const)
                        column.Value = ReadValue(column.Type);
                    if (column.Storage == CpkColumnStorage.Zero)
                        column.Value = ZeroValue(column.Type);
                }
                Columns = columns;

                // Read in the values.
                _tableStream.BaseStream.Position = TableInfo.ValuesOffset;
                Rows = new List<CpkRow>();
                for (var i = 0; i < TableInfo.RowCount; i++)
                {
                    var row = new CpkRow(Columns);

                    foreach (var column in Columns)
                    {
                        if (column.Storage == CpkColumnStorage.Const || column.Storage == CpkColumnStorage.Zero)
                            row[column.Name].Value = column.Value.Value;
                        else if (column.Storage == CpkColumnStorage.Row)
                            row[column.Name].Value = ReadValue(column.Type).Value;
                    }

                    Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// Writes the current <see cref="CpkTable"/> data to the output <see cref="Stream"/>.
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                var valuesOffset = 0;
                var binaryOffset = 0;
                var nameOffset = 0;

                // Write the main header
                bw.WriteStruct(Header);

                // Switch to BE
                bw.ByteOrder = ByteOrder.BigEndian;

                // Write the table info
                bw.WriteStruct(TableInfo);

                #region Strings

                var strings = new Dictionary<string, int>();
                var stringStream = new BinaryWriterX(new MemoryStream());

                void AddString(string str)
                {
                    if (strings.ContainsKey(str)) return;
                    strings.Add(str, (int)stringStream.BaseStream.Position);
                    stringStream.WriteString(str, Encoding.ASCII, false);
                }

                // NULL
                AddString("<NULL>");

                // Table name
                AddString(Name);

                // Column names and constant value strings
                foreach (var column in Columns)
                {
                    AddString(column.Name);
                    if (column.Storage == CpkColumnStorage.Const && column.Type == CpkDataType.String)
                        AddString((string)column.Value.Value);
                }

                // Row column value strings
                foreach (var row in Rows)
                    foreach (var column in Columns)
                        if (column.Storage == CpkColumnStorage.Row && column.Type == CpkDataType.String)
                            AddString((string)row[column.Name].Value);

                // Reset string stream position
                stringStream.BaseStream.Position = 0;

                #endregion

                #region Data

                // TODO: Handle saving binary data columns

                #endregion

                // Write out columns
                foreach (var column in Columns)
                {
                    var flags = (byte)((int)column.Storage ^ (int)column.Type);
                    bw.Write(flags);
                    bw.Write(strings[column.Name]);
                    switch (column.Storage)
                    {
                        case CpkColumnStorage.Const:
                            WriteValue(bw, column.Value, column, strings);
                            break;
                    }
                }

                // Write out rows
                foreach (var row in Rows)
                    foreach (var column in Columns)
                        if (column.Storage == CpkColumnStorage.Row)
                        {
                            WriteValue(bw, row[column.Name], column, strings);
                        }

                // Update Strings Offset and write out the stings
                TableInfo.StringsOffset = (int)bw.BaseStream.Position - 0x18;
                stringStream.BaseStream.CopyTo(bw.BaseStream);
                stringStream.Close();

                // Align to nearest 8 bytes
                bw.WriteAlignment(8);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private int ValueSize(CpkDataType type)
        {
            switch (type)
            {
                case CpkDataType.UInt8:
                case CpkDataType.SInt8:
                    return 1;
                case CpkDataType.UInt16:
                case CpkDataType.SInt16:
                    return 2;
                case CpkDataType.UInt32:
                case CpkDataType.SInt32:
                    return 4;
                case CpkDataType.UInt64:
                case CpkDataType.SInt64:
                    return 8;
                case CpkDataType.Float:
                    return 4;
                case CpkDataType.String:
                    return 4;
                case CpkDataType.Data:
                    return 8;
                default:
                    return 0;
            }
        }

        // Reading

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

        //// Writing - TODO: This needs a rewrite since the "Data" type isn't properly supported.

        /// <summary>
        /// Writes a value based on the given <see cref="CpkValue"/> and <see cref="CpkColumnInfo"/>.
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="val"></param>
        /// <param name="col"></param>
        /// <param name="strings"></param>
        /// <param name="data"></param>
        private void WriteValue(BinaryWriterX bw, CpkValue val, CpkColumnInfo col, Dictionary<string, int> strings = null, Dictionary<int, int> data = null)
        {
            switch (val.Type)
            {
                case CpkDataType.UInt8:
                    bw.Write((byte)val.Value);
                    break;
                case CpkDataType.SInt8:
                    bw.Write((sbyte)val.Value);
                    break;
                case CpkDataType.UInt16:
                    bw.Write((ushort)val.Value);
                    break;
                case CpkDataType.SInt16:
                    bw.Write((short)val.Value);
                    break;
                case CpkDataType.UInt32:
                    bw.Write((uint)val.Value);
                    break;
                case CpkDataType.SInt32:
                    bw.Write((int)val.Value);
                    break;
                case CpkDataType.UInt64:
                    bw.Write((ulong)val.Value);
                    break;
                case CpkDataType.SInt64:
                    bw.Write((long)val.Value);
                    break;
                case CpkDataType.Float:
                    bw.Write((float)val.Value);
                    break;
                case CpkDataType.String:
                    bw.Write(strings[(string)val.Value]);
                    break;
                case CpkDataType.Data:
                    bw.Write((int)val.Value);
                    bw.Write(data[(int)val.Value]);
                    break;
            }
        }
    }
}
