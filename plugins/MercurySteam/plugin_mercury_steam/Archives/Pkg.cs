using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_mercury_steam.Archives
{
    class Pkg
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PkgHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(PkgEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PkgHeader>();

            // Read entries
            var entries = br.ReadMultiple<PkgEntry>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var name = $"{entry.hash:X8}.bin";
                var fileStream = new SubStream(input, entry.startOffset, entry.endOffset - entry.startOffset);

                result.Add(new PkgArchiveFileInfo(fileStream, name, entry.hash));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataAlignment = PkgSupport.DetermineAlignment((files[0] as PkgArchiveFileInfo).Type);
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + dataAlignment - 1) & ~(dataAlignment - 1);

            // Write files
            var entries = new List<PkgEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PkgArchiveFileInfo>())
            {
                // Write file data
                var alignment = PkgSupport.DetermineAlignment(file.Type);
                var alignedDataPosition = (dataPosition + alignment - 1) & ~(alignment - 1);

                output.Position = alignedDataPosition;
                var writtenSize = file.SaveFileData(output);

                // Create entry
                entries.Add(new PkgEntry
                {
                    startOffset = alignedDataPosition,
                    endOffset = (int)(alignedDataPosition + writtenSize),
                    hash = file.Hash
                });

                dataPosition = (int)(alignedDataPosition + writtenSize);
            }
            bw.WriteAlignment(4);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new PkgHeader { fileCount = files.Count, tableSize = dataOffset - 4, dataSize = (int)(output.Length - dataOffset) });
        }
    }
}
