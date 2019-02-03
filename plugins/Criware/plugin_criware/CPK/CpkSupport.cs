using System.Collections.Generic;
using Komponent.IO;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Standard header for CPK tables.
    /// </summary>
    public class CpkTableHeader
    {
        [FixedLength(4)]
        public string Magic;
        public int Const1 = 0xFF;
        public int PacketSize;
        public int Null1;
    }

    /// <summary>
    /// Standard CPK table info block.
    /// </summary>
    public class CpkTableInfo
    {
        [FixedLength(4)]
        public string Utf = "@UTF";
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public int TableSize;
        public int ValuesOffset;
        public int StringsOffset;
        public int BinaryOffset;
        public int NameOffset;
        public short ColumnCount;
        public short RowLength;
        public int RowCount;
    }

    /// <summary>
    /// CPK column info.
    /// </summary>
    public class CpkColumnInfo
    {
        public string Name;
        public CpkColumnStorage Storage;
        public CpkDataType Type;
        public CpkValue Value;
    }

    /// <summary>
    /// CPK row data.
    /// </summary>
    public class CpkRow
    {
        /// <summary>
        /// The dictionary of values stored in the row.
        /// </summary>
        public readonly Dictionary<string, CpkValue> Values = new Dictionary<string, CpkValue>();

        /// <summary>
        /// Value indexer for the row.
        /// </summary>
        /// <param name="column">Column name.</param>
        /// <returns>A CPK Value object.</returns>
        public CpkValue this[string column]
        {
            get => Values[column];
            set => Values[column] = value;
        }

        /// <summary>
        /// Instantiates a new <see cref="CpkRow"/> from a set of <see cref="CpkColumnInfo"/>s.
        /// </summary>
        /// <param name="columns">Column info list.</param>
        public CpkRow(IEnumerable<CpkColumnInfo> columns)
        {
            foreach (var column in columns)
            {
                Values.Add(column.Name, new CpkValue { Type = column.Type } );
            }
        }
    }

    /// <summary>
    /// CPK value storage class.
    /// </summary>
    public class CpkValue
    {
        /// <summary>
        /// The data type of the value;
        /// </summary>
        public CpkDataType Type;

        /// <summary>
        /// The stored value;
        /// </summary>
        public object Value;

        /// <summary>
        /// Dump method for LinqPad.
        /// </summary>
        /// <returns>The value's string representation.</returns>
        private object ToDump() => $"{Value} ({Type.ToString()})";
    }

    /// <summary>
    /// CPK data type enumeration.
    /// </summary>
    public enum CpkDataType : byte
    {
        UInt8 = 0x00,
        SInt8 = 0x01,
        UInt16 = 0x02,
        SInt16 = 0x03,
        UInt32 = 0x04,
        SInt32 = 0x05,
        UInt64 = 0x06,
        SInt64 = 0x07,
        Float = 0x08,
        //Double = 0x09,
        String = 0x0A,
        Data = 0x0B,
    }

    /// <summary>
    /// CPK column storage enumeration.
    /// </summary>
    public enum CpkColumnStorage : byte
    {
        Zero = 0x10,
        Const = 0x30,
        Row = 0x50,
    }
}
