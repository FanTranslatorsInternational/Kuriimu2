using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum.Crc;

namespace plugin_level5.NDS.Archive
{
    public class Gfsp
    {
        private const int HeaderSize_ = 0x14;
        private const int EntrySize_ = 0x8;

        private int _type;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);
            _type = header.ArchiveType;

            // Read entries
            input.Position = header.FileInfoOffset;
            var entries = ReadEntries(br, header.FileCount);

            // Get name stream
            var nameStream = new SubStream(input, header.FilenameTableOffset, header.FilenameTableSize);
            using var nameBr = new BinaryReaderX(nameStream);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, header.DataOffset + entry.FileOffset, entry.FileSize);

                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = nameBr.ReadNullTerminatedString();

                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = fileName
                };

                result.Add(new ArchiveFile(fileInfo));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var crc16 = Crc16.X25;

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileInfoOffset = HeaderSize_;
            var nameOffset = fileInfoOffset + files.Count * EntrySize_;
            var dataOffset = (nameOffset + files.Sum(x => Encoding.ASCII.GetByteCount(x.FilePath.GetName()) + 1) + 3) & ~3;

            // Write files
            var fileInfos = new List<GfspFileInfo>();

            var fileOffset = 0;
            var stringOffset = 0;
            foreach (var file in files)
            {
                output.Position = dataOffset + fileOffset;

                var writtenSize = file.WriteFileData(output, false);
                bw.WriteAlignment(4);

                fileInfos.Add(new GfspFileInfo
                {
                    hash = crc16.ComputeValue(file.FilePath.GetName()),
                    FileOffset = fileOffset,
                    NameOffset = stringOffset,
                    FileSize = (int)writtenSize
                });

                fileOffset += (int)file.FileSize;
                stringOffset += Encoding.ASCII.GetByteCount(file.FilePath.GetName()) + 1;
            }

            // Write names
            output.Position = nameOffset;
            foreach (var name in files.Select(x => x.FilePath.GetName()))
                bw.WriteString(name);

            // Write entries
            output.Position = fileInfoOffset;
            WriteEntries(fileInfos, bw);

            // Write header
            var header = new GfspHeader
            {
                magic = "GFSP",

                FileCount = (ushort)files.Count,
                ArchiveType = _type,

                FileInfoOffset = (ushort)fileInfoOffset,
                FilenameTableOffset = (ushort)nameOffset,
                DataOffset = (ushort)dataOffset,

                FileInfoSize = (ushort)(nameOffset - fileInfoOffset),
                FilenameTableSize = (ushort)(dataOffset - nameOffset),
                DataSize = (uint)(output.Length - dataOffset)
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private GfspHeader ReadHeader(BinaryReaderX reader)
        {
            return new GfspHeader
            {
                magic = reader.ReadString(4),
                fc1 = reader.ReadByte(),
                fc2 = reader.ReadByte(),
                infoOffsetUnshifted = reader.ReadUInt16(),
                nameTableOffsetUnshifted = reader.ReadUInt16(),
                dataOffsetUnshifted = reader.ReadUInt16(),
                infoSizeUnshifted = reader.ReadUInt16(),
                nameTableSizeUnshifted = reader.ReadUInt16(),
                dataSizeUnshifted = reader.ReadUInt32()
            };
        }

        private GfspFileInfo[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new GfspFileInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private GfspFileInfo ReadEntry(BinaryReaderX reader)
        {
            return new GfspFileInfo
            {
                hash = reader.ReadUInt16(),
                tmp = reader.ReadUInt16(),
                size = reader.ReadUInt16(),
                tmp2 = reader.ReadUInt16()
            };
        }

        private void WriteHeader(GfspHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fc1);
            writer.Write(header.fc2);
            writer.Write(header.infoOffsetUnshifted);
            writer.Write(header.nameTableOffsetUnshifted);
            writer.Write(header.dataOffsetUnshifted);
            writer.Write(header.infoSizeUnshifted);
            writer.Write(header.nameTableSizeUnshifted);
            writer.Write(header.dataSizeUnshifted);
        }

        private void WriteEntries(IList<GfspFileInfo> entries, BinaryWriterX writer)
        {
            foreach (GfspFileInfo entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(GfspFileInfo entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.tmp);
            writer.Write(entry.size);
            writer.Write(entry.tmp2);
        }
    }
}
