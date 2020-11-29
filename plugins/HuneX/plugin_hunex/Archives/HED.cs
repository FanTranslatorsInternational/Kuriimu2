using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_hunex.Archives
{
    class HED
    {
        public IList<IArchiveFileInfo> Load(Stream hedStream, Stream mrgStream, Stream namStream = null)
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
            var entrySize = Tools.MeasureType(entryType);
            var entryCount = (int)((hedStream.Length - 0x10) / entrySize);

            // Read entries
            hedStream.Position = 0;
            var entries = hedBr.ReadMultiple(entryCount, entryType);

            // Read names
            var names = (IList<INamEntry>)Array.Empty<INamEntry>();
            if (namStream != null)
            {
                using var namBr = new BinaryReaderX(namStream);
                names = namBr.ReadMultiple(entryCount, nameEntry).Cast<INamEntry>().ToArray();
            }

            // Add files
            var usedNames = new Dictionary<string, int>();

            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = (IHedEntry)entries[i];

                var subStream = new SubStream(mrgStream, entry.Offset, entry.Size);
                var fileName = $"{i:00000000}.bin";

                if (names.Count > 0)
                {
                    var listName = (UPath)names[i].Name;
                    if (!usedNames.ContainsKey(listName.FullName))
                        usedNames[listName.FullName] = 0;
                    fileName = listName.GetNameWithoutExtension() + $"_{usedNames[listName.FullName]++:00}" + listName.GetExtensionWithDot();
                }

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }
    }
}
