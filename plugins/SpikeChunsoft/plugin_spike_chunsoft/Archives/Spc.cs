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

namespace plugin_spike_chunsoft.Archives
{
    class Spc
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<SpcHeader>();

            // Read Root entry
            input.Position = 0x20;
            var rootEntry = br.ReadType<SpcEntry>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < rootEntry.decompSize; i++)
            {
                var entry = br.ReadType<SpcEntry>();

                var fileStream = new SubStream(input, input.Position, entry.compSize);
                var fileName = entry.name;

                input.Position += entry.compSize;
                br.SeekAlignment();

                switch (entry.flag)
                {
                    case 1:
                        result.Add(new ArchiveFileInfo(fileStream, fileName));
                        break;

                    case 2:
                        result.Add(new ArchiveFileInfo(fileStream, fileName, Compressions.Danganronpa3, entry.decompSize));
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown entry flag {entry.flag}.");
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var rootOffset = 0x20;
            var dataOffset = 0x50;

            // Write entries and files
            var dataPosition = (long)dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var fileName = file.FilePath.ToRelative().FullName;
                var entryLength = (0x20 + fileName.Length + 1 + 0xF) & ~0xF;

                // Write file data
                output.Position = dataPosition + entryLength;
                var writtenSize = file.SaveFileData(output);

                bw.WriteAlignment();
                var nextDataPosition = output.Position;

                // Write entry
                output.Position = dataPosition;
                bw.WriteType(new SpcEntry
                {
                    flag = (short)(file.UsesCompression ? 2 : 1),
                    compSize = (int)writtenSize,
                    decompSize = (int)file.FileSize,
                    nameLength = fileName.Length,
                    name = fileName
                });

                dataPosition = nextDataPosition;
            }

            // Write root entry
            output.Position = rootOffset;
            bw.WriteType(new SpcEntry { unk1 = 0, decompSize = files.Count, nameLength = 4, name = "Root" });

            // Write header
            output.Position = 0;
            bw.WriteType(new SpcHeader());
        }
    }
}
