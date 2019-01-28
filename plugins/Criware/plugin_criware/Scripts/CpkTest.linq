<Query Kind="Program">
  <Reference Relative="..\..\..\..\src\Komponent\bin\Debug\Komponent.dll">D:\Development\Kuriimu2_opf\src\Komponent\bin\Debug\Komponent.dll</Reference>
  <Reference Relative="..\..\..\..\src\Komponent\bin\Debug\Kontract.dll">D:\Development\Kuriimu2_opf\src\Komponent\bin\Debug\Kontract.dll</Reference>
  <Reference Relative="..\bin\Debug\plugin_criware.dll">D:\Development\Kuriimu2_opf\plugins\Criware\plugin_criware\bin\Debug\plugin_criware.dll</Reference>
  <Namespace>Komponent.IO</Namespace>
  <Namespace>plugin_criware.CPK</Namespace>
</Query>

void Main()
{
	/// CPK Plugin Tester
	/// v1.0
	/// by Icyson55
	/// Special thanks to @unknownbrackets

	Directory.SetCurrentDirectory(Path.GetDirectoryName(Util.CurrentQueryPath));

	var dataBin = @"D:\PSP\Valkyria Chronicles 3\JPN\PSP_GAME\INSDIR\DATA.BIN.DEC";

	var cpk = new CPK(File.OpenRead(dataBin));
	cpk.TocTable.Rows.Take(10).Dump();
}

/// <summary>
/// Format class that handles CPK archives.
/// </summary>
public class CPK
{
	/// <summary>
	/// The table that stores the header data.
	/// </summary>
	public CpkTable HeaderTable { get; }

	/// <summary>
	/// The table that stores the TOC data.
	/// </summary>
	public CpkTable TocTable { get; }

	/// <summary>
	/// Instantiates a new <see cref="CPK"/> from an input <see cref="Stream"/>.
	/// </summary>
	/// <param name="input"></param>
	public CPK(Stream input)
	{
		// Read in the CPK table.
		HeaderTable = new CpkTable(input);

		var tocOffset = (ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
		var contentOffset = HeaderTable.Rows.First().Values["ContentOffset"].Value;

		// Read in the TOC table.
		input.Position = (long)tocOffset;
		TocTable = new CpkTable(input);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="output"></param>
	/// <returns></returns>
	public bool Save(Stream output)
	{

		return false;
	}
}

// Define other methods and classes here
//
//public class CPK
//{
//	const int HeaderSize = 0x18;
//
//	private BinaryReaderX _tableStream = null;
//	private BinaryReaderX _stringStream = null;
//	private BinaryReaderX _dataStream = null;
//
//	public CpkHeader Header;
//	public CpkTableInfo TableInfo;
//	public CpkTable Table;
//
//	public CPK(Stream input)
//	{
//		using (var br = new BinaryReaderX(input, true))
//		{
//			Header = br.ReadStruct<CpkHeader>();
//			_tableStream = new BinaryReaderX(new SubStream(br.BaseStream, HeaderSize, Header.TableSize), true, ByteOrder.BigEndian);
//
//			// Switch to Big Endian to read the table info
//			br.ByteOrder = ByteOrder.BigEndian;
//			TableInfo = _tableStream.ReadStruct<CpkTableInfo>();
//
//			// Small sanity checks
//			if (Header.PacketSize - 8 != Header.TableSize)
//				throw new FormatException("The packet size doesn't match the table size. The file might be corrupt or encrypted or not a CPK.");
//			if (Header.PacketSize > 100 * 1024 * 1024)
//				throw new FormatException("The packet size is too big. The file might be corrupt or encrypted or not a CPK.");
//
//			// Streams
//			_stringStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.StringsOffset, Header.TableSize - TableInfo.StringsOffset), true, ByteOrder.BigEndian);
//			_dataStream = new BinaryReaderX(new SubStream(_tableStream.BaseStream, TableInfo.DataOffset, Header.TableSize - TableInfo.DataOffset), true, ByteOrder.BigEndian);
//			
//			// Table
//			Table = new CpkTable
//			{
//				Name = ReadString(TableInfo.NameOffset)
//			};
//			
//			// Columns
//			var columns = new List<CpkColumn>();
//			for (var i = 0; i < TableInfo.NumColumns; i++)
//			{
//				var flags = _tableStream.ReadByte();
//				var col = new CpkColumn {
//					Name = ReadString(_tableStream.ReadInt32()),
//					Storage = (CpkColumnStorage)(flags & 0xF0),
//					Type = (CpkDataType)(flags & 0x0F)
//				};
//				columns.Add(col);
//
//				if (col.Storage == CpkColumnStorage.Const)
//					col.Value = ReadValue(col.Type);
//				if (col.Storage == CpkColumnStorage.Zero)
//					col.Value = ZeroValue(col.Type);
//			}
//			Table.Columns = columns;
//
//			// Values
//			_tableStream.BaseStream.Position = TableInfo.ValuesOffset;
//			Table.Rows = new List<CpkRow>();
//			for (var i = 0; i < TableInfo.NumRows; i++)
//			{
//				var row = new CpkRow(Table.Columns);
//				
//				foreach (var col in Table.Columns)
//				{
//					switch (col.Storage)
//					{
//						case CpkColumnStorage.Const:
//						case CpkColumnStorage.Zero:	
//							row[col.Name].Value = col.Value.Value;
//							break;
//						case CpkColumnStorage.Row:
//							row[col.Name].Value = ReadValue(col.Type).Value;
//							break;
//						default:
//							break;
//					}
//				}
//				
//				Table.Rows.Add(row);
//			}
//		}
//		
//		string ReadString(int position)
//		{
//			_stringStream.BaseStream.Position = position;
//			return _stringStream.ReadString();
//		}
//		
//		byte[] ReadData(int position, int length)
//		{
//			_dataStream.BaseStream.Position = position;
//			return _dataStream.ReadBytes(length);
//		}
//
//		CpkValue ReadValue(CpkDataType type)
//		{
//			switch (type)
//			{
//				case CpkDataType.UInt8:
//					return new CpkValue { Type = type, Value = _tableStream.ReadByte() };
//				case CpkDataType.SInt8:
//					return new CpkValue { Type = type, Value = _tableStream.ReadSByte() };
//				case CpkDataType.UInt16:
//					return new CpkValue { Type = type, Value = _tableStream.ReadUInt16() };
//				case CpkDataType.SInt16:
//					return new CpkValue { Type = type, Value = _tableStream.ReadInt16() };
//				case CpkDataType.UInt32:
//					return new CpkValue { Type = type, Value = _tableStream.ReadUInt32() };
//				case CpkDataType.SInt32:
//					return new CpkValue { Type = type, Value = _tableStream.ReadInt32() };
//				case CpkDataType.UInt64:
//					return new CpkValue { Type = type, Value = _tableStream.ReadUInt64() };
//				case CpkDataType.SInt64:
//					return new CpkValue { Type = type, Value = _tableStream.ReadInt64() };
//				case CpkDataType.Float:
//					return new CpkValue { Type = type, Value = _tableStream.ReadSingle() };
//				case CpkDataType.String:
//					return new CpkValue { Type = type, Value = ReadString(_tableStream.ReadInt32()) };
//				case CpkDataType.Data:
//					return new CpkValue { Type = type, Value = ReadData(_tableStream.ReadInt32(), _tableStream.ReadInt32()) };
//				default:
//					return new CpkValue();
//			}
//		}
//
//		CpkValue ZeroValue(CpkDataType type)
//		{
//			switch (type)
//			{
//				case CpkDataType.UInt8:
//					return new CpkValue { Type = type, Value = (byte)0 };
//				case CpkDataType.SInt8:
//					return new CpkValue { Type = type, Value = (sbyte)0 };
//				case CpkDataType.UInt16:
//					return new CpkValue { Type = type, Value = (ushort)0 };
//				case CpkDataType.SInt16:
//					return new CpkValue { Type = type, Value = (short)0 };
//				case CpkDataType.UInt32:
//					return new CpkValue { Type = type, Value = (uint)0 };
//				case CpkDataType.SInt32:
//					return new CpkValue { Type = type, Value = (int)0 };
//				case CpkDataType.UInt64:
//					return new CpkValue { Type = type, Value = (ulong)0 };
//				case CpkDataType.SInt64:
//					return new CpkValue { Type = type, Value = (long)0 };
//				case CpkDataType.Float:
//					return new CpkValue { Type = type, Value = (float)0 };
//				case CpkDataType.String:
//					return new CpkValue { Type = type, Value = null };
//				case CpkDataType.Data:
//					return new CpkValue { Type = type, Value = null };
//				default:
//					return new CpkValue();
//			}
//		}
//	}
//}