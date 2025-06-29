using System.Text;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_criware.Archives.Support;

namespace plugin_criware.Archives
{
    /// <summary>
    /// Standard header for CPK tables.
    /// </summary>
    public class CpkTableHeader
    {
        public string magic;
        public int flags = 0xFF;    // Not encrypted by default
        public int packetSize;
        public int zero0;

        public bool IsEncrypted => (flags & 0xFF) != 0xFF;
    }

    /// <summary>
    /// Standard CPK table info block.
    /// </summary>
    public class CpkTableInfo
    {
        public string magic = "@UTF";
        public int tableSize;
        public int valuesOffset;
        public int stringsOffset;
        public int binaryOffset;
        public int nameOffset;
        public short columnCount;
        public short rowLength;
        public int rowCount;
    }

    class CpkArchiveFile : ArchiveFile
    {
        public CpkRow Row { get; }

        public CpkArchiveFile(ArchiveFileInfo fileInfo, CpkRow row) : base(fileInfo)
        {
            Row = row;
        }
    }

    public class CpkSupport
    {
        public static long Align(long value, int align)
        {
            return (value + (align - 1)) & ~(align - 1);
        }

        public static string ReadString(BinaryReaderX br, long offset)
        {
            br.BaseStream.Position = offset;
            return br.ReadNullTerminatedString();
        }

        public static byte[] ReadBytes(BinaryReaderX br, long offset, int size)
        {
            br.BaseStream.Position = offset;
            return br.ReadBytes(size);
        }

        public static void WriteString(BinaryWriterX bw, long offset, string value)
        {
            bw.BaseStream.Position = offset;
            bw.WriteString(value, Encoding.ASCII, false);
        }

        public static void WriteData(BinaryWriterX bw, long offset, byte[] value)
        {
            bw.BaseStream.Position = offset;
            bw.Write(value);
        }
    }

    /// <summary>
    /// CPK column info.
    /// </summary>
    public class CpkColumnInfo
    {
        public string Name { get; }
        public CpkColumnStorage Storage { get; }
        public CpkDataType Type { get; }
        public CpkValue Value { get; }

        // TODO: Make public
        internal CpkColumnInfo(string name, CpkColumnStorage storage, CpkDataType type, CpkValue value = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Storage = storage;
            Type = type;
            Value = value;
        }

        // TODO: Remove methods and do creation externally
        public static CpkColumnInfo CreateConst(string name, CpkValue value)
        {
            return new CpkColumnInfo(name, CpkColumnStorage.Const, value.Type, value);
        }

        public static CpkColumnInfo CreateZero(string name, CpkDataType type)
        {
            return new CpkColumnInfo(name, CpkColumnStorage.Default, type, CpkValue.Default(type));
        }

        public static CpkColumnInfo CreateRow(string name, CpkDataType type)
        {
            return new CpkColumnInfo(name, CpkColumnStorage.Row, type);
        }
    }

    /// <summary>
    /// CPK row data.
    /// </summary>
    public class CpkRow
    {
        private readonly Dictionary<string, CpkValue> _values;

        /// <summary>
        /// The dictionary of values stored in the row.
        /// </summary>
        public IReadOnlyDictionary<string, CpkValue> Values => _values;

        public CpkRow()
        {
            _values = new Dictionary<string, CpkValue>();
        }

        public void Add(string name, CpkValue value)
        {
            _values[name] = value;
        }

        public TType Get<TType>(string valueName)
        {
            if (!_values.ContainsKey(valueName))
                return default;
            return _values[valueName].Get<TType>();
        }

        public void Set(string valueName, object value)
        {
            _values[valueName].Set(value);
        }
    }

    /// <summary>
    /// CPK data type enumeration.
    /// </summary>
    public enum CpkDataType : byte
    {
        UInt8 = 0x0,
        SInt8 = 0x1,
        UInt16 = 0x2,
        SInt16 = 0x3,
        UInt32 = 0x4,
        SInt32 = 0x5,
        UInt64 = 0x6,
        SInt64 = 0x7,
        Float = 0x8,
        //Double = 0x9,
        String = 0xA,
        Data = 0xB
    }

    /// <summary>
    /// Value storage for columns.
    /// </summary>
    public enum CpkColumnStorage : byte
    {
        /// <summary>
        /// Value of this column is always default.
        /// </summary>
        Default = 0x1,

        /// <summary>
        /// Value of this column is always constant.
        /// </summary>
        Const = 0x3,

        /// <summary>
        /// Value of this column comes from the row.
        /// </summary>
        Row = 0x5
    }
}
