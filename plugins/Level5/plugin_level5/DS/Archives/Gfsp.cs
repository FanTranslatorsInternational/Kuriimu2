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

namespace plugin_level5.DS.Archives
{
    class Gfsp
    {
        private static int HeaderSize = Tools.MeasureType(typeof(GfspHeader));
        private static int EntrySize = Tools.MeasureType(typeof(GfspFileInfo));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<GfspHeader>();

            // Read entries
            input.Position = header.FileInfoOffset;
            var entries = br.ReadMultiple<GfspFileInfo>(header.FileCount);

            // Get name stream
            var nameStream = new SubStream(input, header.FilenameTableOffset, header.FilenameTableSize);
            using var nameBr = new BinaryReaderX(nameStream);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, header.DataOffset + entry.FileOffset, entry.size);

                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = nameBr.ReadCStringASCII();

                result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc16 = Crc16.X25;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileInfoOffset = HeaderSize;
            var nameOffset = fileInfoOffset + files.Count * EntrySize;
            var dataOffset = (nameOffset + files.Sum(x => Encoding.ASCII.GetByteCount(x.FilePath.GetName()) + 1) + 3) & ~3;

            // Write files
            var fileInfos = new List<GfspFileInfo>();

            var fileOffset = 0;
            var stringOffset = 0;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = dataOffset + fileOffset;

                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(4);

                fileInfos.Add(new GfspFileInfo
                {
                    hash = crc16.ComputeValue(file.FilePath.GetName()),
                    FileOffset = fileOffset,
                    NameOffset = stringOffset,
                    size = (ushort)writtenSize
                });

                fileOffset += (int)file.FileSize;
                stringOffset += Encoding.ASCII.GetByteCount(file.FilePath.GetName()) + 1;
            }

            // Write names
            output.Position = nameOffset;
            foreach (var name in files.Select(x => x.FilePath.GetName()))
                bw.WriteString(name, Encoding.ASCII, false);

            // Write entries
            output.Position = fileInfoOffset;
            bw.WriteMultiple(fileInfos);

            // Write header
            var header = new GfspHeader
            {
                FileCount = (ushort)files.Count,

                FileInfoOffset = (ushort)fileInfoOffset,
                FilenameTableOffset = (ushort)nameOffset,
                DataOffset = (ushort)dataOffset,

                FileInfoSize = (ushort)(nameOffset - fileInfoOffset),
                FilenameTableSize = (ushort)(dataOffset - nameOffset),
                DataSize = (uint)(output.Length - dataOffset)
            };

            output.Position = 0;
            bw.WriteType(header);
        }
    }
}
