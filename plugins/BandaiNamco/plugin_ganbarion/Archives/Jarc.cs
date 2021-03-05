using System;
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

namespace plugin_ganbarion.Archives
{
    class Jarc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(JarcHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(JarcEntry));

        private JarcHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<JarcHeader>();

            // Read entries
            var entries = br.ReadMultiple<JarcEntry>(_header.fileCount);

            // Read files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                input.Position = entry.nameOffset;

                var fileStream = new SubStream(input, entry.fileOffset, entry.fileSize);
                var name = br.ReadCStringASCII();

                result.Add(new JarcArchiveFileInfo(fileStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc32 = Crc32.Default;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var nameOffset = entryOffset + files.Count * EntrySize;
            var dataOffset = (nameOffset + 0x7F) & ~0x7F;

            // Write files
            var namePosition = nameOffset;
            var dataPosition = dataOffset;

            var entries = new List<JarcEntry>();
            foreach (var file in files.Cast<JarcArchiveFileInfo>())
            {
                output.Position = dataPosition;
                file.SaveFileData(output);

                entries.Add(new JarcEntry
                {
                    fileOffset = dataPosition,
                    nameOffset = namePosition,
                    fileSize = (int)file.FileSize,
                    hash = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(Encoding.ASCII.GetBytes(file.FilePath.ToRelative().FullName))),
                    unk1 = file.Entry.unk1
                });

                dataPosition += (int)((file.FileSize + 0x7F) & ~0x7F);
                namePosition += Encoding.ASCII.GetByteCount(file.FilePath.ToRelative().FullName) + 1;
            }

            // Write names
            output.Position = nameOffset;
            foreach (var file in files)
                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.fileCount = files.Count;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
