using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_square_enix.Archives
{
    class Dpk
    {
        private const int BlockSize = 0x80;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<DpkHeader>();

            // Read entries
            input.Position = BlockSize;
            var entries = br.ReadMultiple<DpkEntry>(header.fileCount);

            var ordered = entries.OrderBy(x => x.name.Trim('\0')).ToArray();

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.compSize);
                var fileName = entry.name.Trim('\0');

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = BlockSize;
            var fileOffset = entryOffset + files.Count * BlockSize;

            // Write files
            // HINT: Files are ordered by name
            var entries = new List<DpkEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.OrderBy(x => x.FilePath.ToRelative().FullName).Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                var name = CapAndPadName(file.FilePath.ToRelative().FullName);
                entries.Add(new DpkEntry
                {
                    name = name,
                    nameSum = (short)NameSum(name),
                    offset = filePosition,
                    compSize = (int)writtenSize,
                    decompSize = (int)file.FileSize
                });

                filePosition += (int)((writtenSize + (BlockSize - 1)) & ~(BlockSize - 1));
            }

            // Write entries
            // HINT: Entries are ordered by nameSum
            output.Position = entryOffset;
            bw.WriteMultiple(entries.OrderBy(x => x.nameSum));

            // Write header
            output.Position = 0;
            bw.WriteType(new DpkHeader
            {
                fileCount = files.Count,
                fileSize = (int)output.Length
            });
        }

        private IArchiveFileInfo CreateAfi(Stream file, string name, DpkEntry entry)
        {
            if (entry.decompSize != entry.compSize)
                return new ArchiveFileInfo(file, name, Compressions.Wp16, entry.decompSize)
                {
                    PluginIds = Path.GetExtension(name) == ".PCK" ? new[] { Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3") } : null
                };

            return new ArchiveFileInfo(file, name)
            {
                PluginIds = Path.GetExtension(name) == ".PCK" ? new[] { Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3") } : null
            };
        }

        private string CapAndPadName(string input)
        {
            return input.PadRight(0x16, '\0')[..0x16];
        }

        private int NameSum(string input)
        {
            return input.Sum(x => (byte)x);
        }
    }
}
