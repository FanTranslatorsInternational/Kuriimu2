using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_level5.Mobile.Archives
{
    class Arc1
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(Arc1Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(Arc1FileEntry));

        private Arc1Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            // Read header
            using var headerBr = new BinaryReaderX(new Arc1CryptoStream(input, 0), true);
            _header = headerBr.ReadType<Arc1Header>();

            // Prepare file info stream
            var infoStream = new Arc1CryptoStream(new SubStream(input, _header.entryOffset, _header.entrySize), (uint)_header.entryOffset);
            using var infoBr = new BinaryReaderX(infoStream);

            // Read entries
            var entryCount = infoBr.ReadInt32();
            var entries = infoBr.ReadMultiple<Arc1FileEntry>(entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                infoStream.Position = entry.nameOffset;
                var name = infoBr.ReadCStringASCII();

                var fileStream = new SubStream(input, entry.offset, entry.size);

                result.Add(new Arc1ArchiveFileInfo(fileStream, name, Path.GetExtension(name) != ".mp4", (uint)entry.offset));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Calculate offsets
            var dataOffset = HeaderSize;
            var entryOffset = dataOffset + files.Sum(x => x.FileSize);
            var stringOffset = entryOffset + 4 + files.Count * EntrySize;
            var totalSize = stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1);

            // Prepare output stream
            output.SetLength(totalSize);

            // Write files
            var dataPosition = dataOffset;
            var stringPosition = stringOffset - entryOffset;

            var entries = new List<Arc1FileEntry>();
            foreach (var file in files.Cast<Arc1ArchiveFileInfo>())
            {
                // Write file data
                Stream outputRegion = new SubStream(output, dataPosition, file.FileSize);
                if (file.ContentChanged && file.FilePath.GetExtensionWithDot() != ".mp4")
                    outputRegion = new Arc1CryptoStream(outputRegion, (uint)dataPosition);

                file.GetFinalStream().CopyTo(outputRegion);

                // Add entry
                entries.Add(new Arc1FileEntry
                {
                    nameOffset = (int)stringPosition,
                    offset = dataPosition,
                    size = (int)file.FileSize
                });

                dataPosition += (int)file.FileSize;
                stringPosition += file.FilePath.ToRelative().FullName.Length + 1;
            }

            // Write entry information
            var infoStream = new Arc1CryptoStream(new SubStream(output, entryOffset, totalSize - entryOffset), (uint)entryOffset);
            using var infoBw = new BinaryWriterX(infoStream);

            infoBw.Write(files.Count);
            infoBw.WriteMultiple(entries);

            foreach (var name in files.Select(x => x.FilePath.ToRelative().FullName))
                infoBw.WriteString(name, Encoding.ASCII, false);

            // Write header
            var headerStream = new Arc1CryptoStream(new SubStream(output, 0, HeaderSize), 0);
            using var headerBw = new BinaryWriterX(headerStream);

            _header.entryOffset = (int)entryOffset;
            _header.entrySize = (int)(totalSize - entryOffset);
            _header.fileSize = (int)totalSize;

            headerBw.WriteType(_header);
        }
    }
}
