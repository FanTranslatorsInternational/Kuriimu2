using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;

namespace plugin_level5.Mobile.Archives
{
    class Hp10
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(Hp10Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(Hp10FileEntry));

        private Hp10Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<Hp10Header>();

            // Read entries
            var entries = br.ReadMultiple<Hp10FileEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                input.Position = _header.stringOffset + entry.nameOffset;
                var name = br.ReadCStringASCII();
                var fileStream = new SubStream(input, _header.dataOffset + entry.fileOffset, entry.fileSize);

                result.Add(new Hp10ArchiveFileInfo(fileStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc32b = Crc32.Crc32B;
            var crc32c = Crc32.Crc32C;

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + files.Count * EntrySize;
            var dataOffset = (stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1) + 0x7FF) & ~0x7FF;

            // Write files
            var dataPosition = (long)dataOffset;
            var namePosition = 0;

            var entries = new List<Hp10FileEntry>();
            var strings = new List<string>();
            foreach (var file in files.Cast<Hp10ArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(0x800);

                // Update entry
                file.Entry.crc32bFileNameHash = BinaryPrimitives.ReadUInt32BigEndian(crc32b.Compute(Encoding.ASCII.GetBytes(file.FilePath.GetName())));
                file.Entry.crc32cFileNameHash = BinaryPrimitives.ReadUInt32BigEndian(crc32c.Compute(Encoding.ASCII.GetBytes(file.FilePath.GetName())));
                file.Entry.crc32bFilePathHash = BinaryPrimitives.ReadUInt32BigEndian(crc32b.Compute(Encoding.ASCII.GetBytes(file.FilePath.ToRelative().FullName)));
                file.Entry.crc32cFilePathHash = BinaryPrimitives.ReadUInt32BigEndian(crc32c.Compute(Encoding.ASCII.GetBytes(file.FilePath.ToRelative().FullName)));
                file.Entry.nameOffset = namePosition;
                file.Entry.fileOffset = (uint)(dataPosition - dataOffset);
                file.Entry.fileSize = (int)writtenSize;
                entries.Add(file.Entry);
                strings.Add(file.FilePath.ToRelative().FullName);

                // Update positions
                namePosition += file.FilePath.ToRelative().FullName.Length + 1;
                dataPosition = (dataPosition + writtenSize + 0x7FF) & ~0x7FF;
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var name in strings)
                bw.WriteString(name, Encoding.ASCII, false);
            var stringEndOffset = output.Position;

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.dataOffset = dataOffset;
            _header.stringOffset = stringOffset;
            _header.fileCount = files.Count;
            _header.fileSize = (uint)output.Length;
            _header.stringEnd = (int)stringEndOffset;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
