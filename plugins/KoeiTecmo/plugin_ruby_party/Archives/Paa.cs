using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_ruby_party.Archives
{
    class Paa
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(PaaEntry));

        private PaaHeader _header;

        public IList<IArchiveFileInfo> Load(Stream binStream, Stream arcStream)
        {
            using var binBr = new BinaryReaderX(binStream);

            // Read header
            _header = binBr.ReadType<PaaHeader>();

            // Read entries
            binStream.Position = _header.entryOffset;
            var entries = binBr.ReadMultiple<PaaEntry>(_header.fileCount);

            // Read offsets
            binStream.Position = _header.offsetsOffset;
            var offsets = binBr.ReadMultiple<int>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];
                var offset = offsets[i];

                var subStream = new SubStream(arcStream, offset, entry.size);

                binStream.Position = entry.nameOffset;
                var fileName = binBr.ReadCStringASCII();

                result.Add(new PaaArchiveFileInfo(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream binOutput, Stream arcOutput, IList<IArchiveFileInfo> files)
        {
            using var binBw = new BinaryWriterX(binOutput);
            using var arcBw = new BinaryWriterX(arcOutput);

            // Calculate offsets
            var fileOffset = 0x10;
            var entryOffset = 0x20;
            var offsetsOffset = entryOffset + files.Count * EntrySize;
            var stringOffset = offsetsOffset + ((files.Count * 4 + 0xF) & ~0xF);

            // Write files
            var offsets = new List<int>();
            var entries = new List<PaaEntry>();

            var filePosition = fileOffset;
            var stringPosition = stringOffset;
            foreach (var file in files.Cast<PaaArchiveFileInfo>())
            {
                arcOutput.Position = filePosition;
                var writtenSize = file.SaveFileData(arcOutput);
                arcBw.WriteAlignment();

                file.Entry.size = (int)writtenSize;
                file.Entry.nameOffset = stringPosition;

                offsets.Add(filePosition);
                entries.Add(file.Entry);

                filePosition += ((int)writtenSize + 0xF) & ~0xF;
                stringPosition += (file.FilePath.ToRelative().FullName.Length + 1 + 0xF) & ~0xF;
            }

            // Write strings
            binOutput.Position = stringOffset;
            foreach (var file in files)
            {
                binBw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);
                binBw.WriteAlignment();
            }

            // Write offsets
            binOutput.Position = offsetsOffset;
            binBw.WriteMultiple(offsets);

            // Write entries
            binOutput.Position = entryOffset;
            binBw.WriteMultiple(entries);

            // Write header
            binOutput.Position = 0;

            _header.fileCount = files.Count;
            _header.entryOffset = entryOffset;
            _header.offsetsOffset = offsetsOffset;
            _header.unk2 = _header.fileCount / 2;
            binBw.WriteType(_header);
        }
    }
}
