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

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<TarcHeader>();

            // Read entries
            input.Position = _header.entryOffset;
            var entries = br.ReadMultiple<TarcEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.fileOffset, entry.compSize);

                input.Position = entry.nameOffset;
                var fileName = br.ReadCStringASCII();

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
            var stringOffset = entryOffset + files.Count * 0x20;
            var fileOffset = (stringOffset + stringMap.Keys.Sum(x => x.Length + 1) + 0xF) & ~0xF;

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
                file.Entry.nameOffset = stringMap[file.FilePath.ToRelative().FullName]+stringOffset;
                entries.Add(file.Entry);

                filePosition += (int)((writtenSize + 0xF) & ~0xF);
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var name in stringMap.Keys)
                bw.WriteString(name, Encoding.ASCII, false);
            var stringSecSize = (int)output.Position - stringOffset;

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
