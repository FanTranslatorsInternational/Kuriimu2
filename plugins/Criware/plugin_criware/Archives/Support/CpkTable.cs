using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.IO;
using Kryptography;

namespace plugin_criware.Archives.Support
{
    /// <summary>
    /// Format class that handles CPK Tables.
    /// </summary>
    public class CpkTable
    {
        // HINT: This is a pre-calculated XOR-Pad, based on the actual algorithm for de-/encryption.
        // It is easier to apply a single XOR operation in parallel, instead of sequentially applying XOR on a single byte
        // and then advancing 2 stepping values, which is how the algorithm works.
        private static readonly byte[] XorKey =
        {
            0x5F, 0xCB, 0xA7, 0xB3, 0xAF, 0x5B, 0x77, 0xC3, 0xFF, 0xEB, 0x47, 0xD3, 0x4F, 0x7B, 0x17, 0xE3, 0x9F, 0x0B,
            0xE7, 0xF3, 0xEF, 0x9B, 0xB7, 0x03, 0x3F, 0x2B, 0x87, 0x13, 0x8F, 0xBB, 0x57, 0x23, 0xDF, 0x4B, 0x27, 0x33,
            0x2F, 0xDB, 0xF7, 0x43, 0x7F, 0x6B, 0xC7, 0x53, 0xCF, 0xFB, 0x97, 0x63, 0x1F, 0x8B, 0x67, 0x73, 0x6F, 0x1B,
            0x37, 0x83, 0xBF, 0xAB, 0x07, 0x93, 0x0F, 0x3B, 0xD7, 0xA3
        };

        private static readonly int HeaderSize = Tools.MeasureType(typeof(CpkTableHeader));
        private static readonly int TableInfoSize = Tools.MeasureType(typeof(CpkTableInfo));

        private IList<CpkColumnInfo> _columns;

        /// <summary>
        /// The magic ID the table has.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The list of rows in the table.
        /// </summary>
        public IList<CpkRow> Rows { get; }

        private CpkTable(string id, string name, IList<CpkColumnInfo> columns, IList<CpkRow> rows)
        {
            _columns = columns;

            Id = id;
            Name = name;
            Rows = rows;
        }

        #region Create

        public static CpkTable Create(Stream input, long offset)
        {
            input.Position = offset;
            using var br = new BinaryReaderX(input, true);

            // Read table header
            var header = br.ReadType<CpkTableHeader>();

            // Create UTF stream
            Stream utfStream = new SubStream(input, offset + 0x10, header.packetSize);
            if (header.IsEncrypted)
                utfStream = new XorStream(utfStream, XorKey);

            return Create(utfStream, header.magic);
        }

        public static CpkTable Create(Stream utfStream, string tableMagic)
        {
            // Read table info
            using var utfBr = new BinaryReaderX(utfStream, ByteOrder.BigEndian);
            var tableInfo = utfBr.ReadType<CpkTableInfo>();

            // Create readers
            var tableStream = new SubStream(utfStream, 0x8, tableInfo.tableSize);
            var stringStream = new SubStream(tableStream, tableInfo.stringsOffset,
                tableInfo.binaryOffset - tableInfo.stringsOffset);
            var dataStream = new SubStream(tableStream, tableInfo.binaryOffset,
                tableInfo.tableSize - tableInfo.binaryOffset);

            using var tableBr = new BinaryReaderX(tableStream, ByteOrder.BigEndian);
            using var stringBr = new BinaryReaderX(stringStream, ByteOrder.BigEndian);
            using var dataBr = new BinaryReaderX(dataStream, ByteOrder.BigEndian);

            // Read table name
            var name = CpkSupport.ReadString(stringBr, tableInfo.nameOffset);

            // Read columns
            tableStream.Position = 0x18;

            var columns = new CpkColumnInfo[tableInfo.columnCount];
            for (var i = 0; i < tableInfo.columnCount; i++)
                columns[i] = ReadColumn(tableBr, stringBr, dataBr);

            // Read rows
            tableStream.Position = tableInfo.valuesOffset;

            var rows = new List<CpkRow>(tableInfo.rowCount);
            for (var i = 0; i < tableInfo.rowCount; i++)
                rows.Add(ReadRow(tableBr, stringBr, dataBr, columns));

            return new CpkTable(tableMagic, name, columns, rows);
        }

        #endregion

        #region Write

        public void Write(Stream output, long offset, int align, bool writeHeader = true)
        {
            output.Position = offset;
            using var bw = new BinaryWriterX(output, true, ByteOrder.BigEndian);

            // Creating column infos
            var columns = CreateColumns(Rows);

            // Calculate offsets
            var headerOffset = offset;
            var tableInfoOffset = headerOffset + (writeHeader ? HeaderSize : 0);
            var tableOffset = tableInfoOffset + TableInfoSize;
            var stringOffset = tableOffset + CalculateTableSize(columns, Rows);
            var dataOffset = (stringOffset + CalculateStringTableSize(Name, columns, Rows) + 0xF) & ~0xF;

            var tablePosition = tableOffset;
            var dataPosition = dataOffset;
            var endOffset = dataPosition + CalculateDataTableSize(columns, Rows);

            var stringWriter = new StringWriter(bw, stringOffset);

            // Write <NULL> string
            stringWriter.WriteString("<NULL>");

            // Write table name
            var tableNameOffset = stringWriter.WriteString(Name);

            // Write columns
            output.Position = tableOffset;
            foreach (var column in columns)
            {
                var flags = (byte)(((int)column.Storage << 4) | (int)column.Type);

                // Write column name
                var columnNameOffset = stringWriter.WriteString(column.Name);

                // Write column information
                bw.BaseStream.Position = tablePosition;
                bw.Write(flags);
                bw.Write((int)columnNameOffset);

                tablePosition += 5;

                // Write constant value
                if (column.Storage == CpkColumnStorage.Const)
                    column.Value.Write(bw, stringWriter, ref tablePosition, ref dataPosition, dataOffset);
            }

            // Write rows
            var valuesOffset = tablePosition;
            var rowColumnNames = columns.Where(x => x.Storage == CpkColumnStorage.Row).Select(x => x.Name);
            foreach (var row in Rows)
                foreach (var rowValue in row.Values.Where(x => rowColumnNames.Contains(x.Key)))
                    rowValue.Value.Write(bw, stringWriter, ref tablePosition, ref dataPosition, dataOffset);

            // Write copyright (c)CRI for CpkHeader
            if (Name == "CpkHeader")
            {
                bw.BaseStream.Position = endOffset + 6;
                bw.WriteAlignment(align);

                bw.BaseStream.Position -= 6;
                bw.WriteString("(c)CRI", Encoding.ASCII, false, false);
            }

            // Write table info
            var tableInfo = new CpkTableInfo
            {
                tableSize = (int)(dataPosition - tableInfoOffset - 0x8),
                valuesOffset = (int)(valuesOffset - tableInfoOffset - 0x8),
                stringsOffset = (int)(stringOffset - tableInfoOffset - 0x8),
                binaryOffset = (int)(dataOffset - tableInfoOffset - 0x8),
                nameOffset = (int)tableNameOffset,
                columnCount = (short)columns.Length,
                rowLength = (short)((tablePosition - valuesOffset) / Rows.Count),
                rowCount = Rows.Count
            };

            bw.BaseStream.Position = tableInfoOffset;
            bw.WriteType(tableInfo);

            if (!writeHeader)
                return;

            // Write header
            bw.ByteOrder = ByteOrder.LittleEndian;
            var header = new CpkTableHeader
            {
                magic = Id,
                packetSize = (int)(dataPosition - offset - 0x10)
            };

            bw.BaseStream.Position = headerOffset;
            bw.WriteType(header);
        }

        public int CalculateSize(bool writeHeader = true)
        {
            var columns = CreateColumns(Rows);

            var size = (writeHeader ? HeaderSize : 0) + TableInfoSize + CalculateTableSize(columns, Rows);
            size = (size + CalculateStringTableSize(Name, columns, Rows) + 0xF) & ~0xF;
            size = (size + CalculateDataTableSize(columns, Rows) + 0xF) & ~0xF;

            return size;
        }

        #endregion

        #region Reading

        private static CpkColumnInfo ReadColumn(BinaryReaderX tableBr, BinaryReaderX stringBr, BinaryReaderX dataBr)
        {
            // Read column info
            var flags = tableBr.ReadByte();
            var nameOffset = tableBr.ReadInt32();

            // Resolve final information
            var name = CpkSupport.ReadString(stringBr, nameOffset);
            var storage = (CpkColumnStorage)(flags >> 4);
            var type = (CpkDataType)(flags & 0xF);

            switch (storage)
            {
                case CpkColumnStorage.Const:
                    return CpkColumnInfo.CreateConst(name, CpkValue.Read(type, tableBr, stringBr, dataBr));

                case CpkColumnStorage.Default:
                    return CpkColumnInfo.CreateZero(name, type);

                case CpkColumnStorage.Row:
                    return CpkColumnInfo.CreateRow(name, type);

                default:
                    throw new InvalidOperationException($"Unknown storage type '{storage}'.");
            }
        }

        private static CpkRow ReadRow(BinaryReaderX tableBr, BinaryReaderX stringBr, BinaryReaderX dataBr,
            IList<CpkColumnInfo> columns)
        {
            var row = new CpkRow();

            foreach (var column in columns)
            {
                if (column.Storage == CpkColumnStorage.Row)
                {
                    row.Add(column.Name, CpkValue.Read(column.Type, tableBr, stringBr, dataBr));
                    continue;
                }

                row.Add(column.Name, column.Value);
            }

            return row;
        }

        #endregion

        #region Writing

        private CpkColumnInfo[] CreateColumns(IList<CpkRow> rows)
        {
            var result = new List<CpkColumnInfo>(rows[0].Values.Count);

            foreach (var (name, value) in rows[0].Values)
            {
                var storage = GetStorage(name, rows);
                result.Add(new CpkColumnInfo(name, storage, value.Type, value));
            }

            return result.ToArray();
        }

        private CpkColumnStorage GetStorage(string name, IList<CpkRow> rows)
        {
            // Storage type priorities: Row < Const < Default

            // Only downgrade storage types as per priority
            // Upgrading any storage type from Row to Const/Default, or from Const to Default may override specifically set behaviour for the CPK
            // This will miss out on potential space efficiency, but we should keep the original storage types as much as possible

            var column = _columns.FirstOrDefault(x => x.Name == name);
            if (column == null)
                throw new InvalidOperationException($"Column {name} is not specified in table with ID {Id}.");

            // If storage is Row, just return that, since it has the lowest priority
            if (column.Storage == CpkColumnStorage.Row)
                return CpkColumnStorage.Row;

            var isDefault = rows.All(x => x.Values[name].IsDefault());

            var constValue = rows[0].Values[name].Get<object>();
            var isConst = rows.All(x => x.Values[name].Get<object>()?.Equals(constValue) ?? constValue == null);

            // Check for downgrade from Default to Const or Row
            if (column.Storage == CpkColumnStorage.Default && !isDefault)
                return isConst ? CpkColumnStorage.Const : CpkColumnStorage.Row;

            // Check for downgrade from Const
            if (column.Storage == CpkColumnStorage.Const && !isConst)
                return CpkColumnStorage.Row;

            return column.Storage;
        }

        private int CalculateTableSize(IList<CpkColumnInfo> columns, IList<CpkRow> rows)
        {
            var columnSize = columns.Sum(x => 5 + (x.Storage == CpkColumnStorage.Const ? x.Value.GetSize() : 0));

            var rowColumnNames = columns.Where(x => x.Storage == CpkColumnStorage.Row).Select(x => x.Name);
            var rowSize = rows.Sum(x => rowColumnNames.Sum(y => x.Values[y].GetSize()));

            return columnSize + rowSize;
        }

        private int CalculateStringTableSize(string name, IList<CpkColumnInfo> columns, IList<CpkRow> rows)
        {
            var rowColumnNames = columns.Where(x => x.Storage == CpkColumnStorage.Row || x.Storage == CpkColumnStorage.Const).Select(x => x.Name);
            var strings = rows.SelectMany(x => rowColumnNames.Where(y => x.Values[y].Type == CpkDataType.String && x.Values[y].Get<string>() != null)
                .Select(y => x.Values[y].Get<string>())).Concat(columns.Select(x => x.Name)).Concat(new[] { "<NULL>", name }).Distinct();

            var ascii = Encoding.ASCII;
            return strings.Sum(x => ascii.GetByteCount(x) + 1);
        }

        private int CalculateDataTableSize(IList<CpkColumnInfo> columns, IList<CpkRow> rows)
        {
            var rowColumnNames = columns.Where(x => x.Storage == CpkColumnStorage.Row).Select(x => x.Name);
            var result = rows.Sum(x => rowColumnNames.Sum(y => x.Values[y].Type == CpkDataType.Data && x.Values[y].Get<byte[]>() != null ? x.Values[y].Get<byte[]>().Length : 0));

            return result;
        }

        #endregion
    }
}
