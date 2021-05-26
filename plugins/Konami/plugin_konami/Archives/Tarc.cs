using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using plugin_konami.Compression;

namespace plugin_konami.Archives
{
    class Tarc
    {
        private TarcHeader _header;
        private bool _hasNames;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<TarcHeader>();
            _hasNames = _header.nameOffset != 0;

            // Read entries
            input.Position = _header.entryOffset;
            var entries = br.ReadMultiple<TarcEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.fileOffset, entry.compSize);

                var fileName = $"{i:00000000}.bin";
                if (_hasNames)
                {
                    input.Position = entry.nameOffset;
                    fileName = br.ReadCStringASCII();
                }

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Distinct strings
            var stringMap = new Dictionary<string, int>();

            var stringPosition = 0;
            foreach (var name in files.Select(x => x.FilePath.ToRelative().FullName))
            {
                if (stringMap.ContainsKey(name))
                    continue;

                stringMap[name] = stringPosition;
                stringPosition += name.Length + 1;
            }

            // Calculate offsets
            var entryOffset = 0x30;
            var entrySize = files.Count * 0x20;
            var stringOffset = _hasNames ? entryOffset + entrySize : 0;
            var fileOffset = _hasNames ? (stringOffset + stringMap.Keys.Sum(x => x.Length + 1) + 0xF) & ~0xF : entryOffset + entrySize;

            // Write files
            var entries = new List<TarcEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<TarcArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                // Update entry
                file.Entry.fileOffset = filePosition;
                file.Entry.compSize = (int)writtenSize;
                file.Entry.decompSize = (int)file.FileSize;
                file.Entry.nameOffset = _hasNames ? stringMap[file.FilePath.ToRelative().FullName] + stringOffset : 0;
                entries.Add(file.Entry);

                filePosition += (int)((writtenSize + 0xF) & ~0xF);
            }

            // Write strings
            if (_hasNames)
            {
                output.Position = stringOffset;
                foreach (var name in stringMap.Keys)
                    bw.WriteString(name, Encoding.ASCII, false);
            }
            var stringSecSize = _hasNames ? (int)output.Position - stringOffset : 0;

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;

            _header.fileCount = files.Count;
            _header.entryOffset = entryOffset;
            _header.nameOffset = stringOffset;
            _header.entrySecSize = files.Count * 0x20;
            _header.nameSecSize = stringSecSize;
            _header.fileSize = (int)output.Length;

            bw.WriteType(_header);
        }

        private IArchiveFileInfo CreateAfi(Stream file, string name, TarcEntry entry)
        {
            if (entry.compSize != 0 && entry.compSize != entry.decompSize)
            {
                var method = NintendoCompressor.PeekCompressionMethod(file);
                return new TarcArchiveFileInfo(file, name, entry, NintendoCompressor.GetConfiguration(method), entry.decompSize);
            }

            return new TarcArchiveFileInfo(file, name, entry);
        }
    }
}
