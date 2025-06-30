using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_hunex.Archives
{
    // Specifications: https://github.com/Hintay/PS-HuneX_Tools/tree/master/Specifications
    class HED
    {
        public List<IArchiveFile> Load(Stream hedStream, Stream mrgStream, Stream namStream = null)
        {
            using var hedBr = new BinaryReaderX(hedStream);
            using var mrgBr = new BinaryReaderX(mrgStream, true);

            // Determine entry type
            // HedEntry1 stores the offset as an int and since the first offset is 0, the high 16 bits can only be 0
            // Otherwise we may deal with smaller entry HedEntry2
            var firstOffset = hedBr.ReadInt32() & 0xFFFF0000;
            var entryType = firstOffset > 0 ? typeof(HedEntry2) : typeof(HedEntry1);
            var nameEntry = firstOffset > 0 ? typeof(NamEntry2) : typeof(NamEntry1);

            // Determine entry count
            var entrySize = firstOffset > 0 ? 0x8 : 0x10;
            var entryCount = (int)((hedStream.Length - 0x10) / entrySize);

            // Read entries
            hedStream.Position = 0;
            var entries = ReadEntries(hedBr, entryCount, firstOffset <= 0);

            // Read names
            var names = (IList<INamEntry>)Array.Empty<INamEntry>();
            if (namStream != null)
            {
                using var namBr = new BinaryReaderX(namStream);
                names = ReadNameEntries(namBr, entryCount, firstOffset <= 0);
            }

            // Add files
            var usedNames = new Dictionary<string, int>();

            var result = new List<IArchiveFile>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(mrgStream, entry.Offset, entry.Size);
                var fileName = $"{i:00000000}.bin";

                if (names.Count > 0)
                {
                    var listName = (UPath)names[i].Name;
                    if (!usedNames.ContainsKey(listName.FullName))
                        usedNames[listName.FullName] = 0;
                    fileName = listName.GetNameWithoutExtension() + $"_{usedNames[listName.FullName]++:00}" + listName.GetExtensionWithDot();
                }

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        private IHedEntry[] ReadEntries(BinaryReaderX reader, int count, bool isEntry1)
        {
            var result = new IHedEntry[count];

            for (var i = 0; i < count; i++)
            {
                if (isEntry1)
                    result[i] = ReadEntry1(reader);
                else
                    result[i] = ReadEntry2(reader);
            }

            return result;
        }

        private HedEntry1 ReadEntry1(BinaryReaderX reader)
        {
            return new HedEntry1
            {
                lowOffset = reader.ReadUInt16(),
                highOffset = reader.ReadUInt16(),
                sectorCount = reader.ReadUInt16(),
                lowSize = reader.ReadUInt16()
            };
        }

        private HedEntry2 ReadEntry2(BinaryReaderX reader)
        {
            return new HedEntry2
            {
                lowOffset = reader.ReadUInt16(),
                offsetSize = reader.ReadUInt16()
            };
        }

        private INamEntry[] ReadNameEntries(BinaryReaderX reader, int count, bool isEntry1)
        {
            var result = new INamEntry[count];

            for (var i = 0; i < count; i++)
            {
                if (isEntry1)
                    result[i] = ReadNameEntry1(reader);
                else
                    result[i] = ReadNameEntry2(reader);
            }

            return result;
        }

        private NamEntry1 ReadNameEntry1(BinaryReaderX reader)
        {
            return new NamEntry1
            {
                name = reader.ReadString(0x20, Encoding.GetEncoding("Shift-JIS")).Replace("\r\n", "").Trim('\0')
            };
        }

        private NamEntry2 ReadNameEntry2(BinaryReaderX reader)
        {
            return new NamEntry2
            {
                name = reader.ReadString(0x8, Encoding.GetEncoding("Shift-JIS")).Trim('\0')
            };
        }
    }
}
