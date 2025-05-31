using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.N3DS.Archive
{
    class Bam
    {
        private const int SubHeaderSize = 12;

        private BamHeader _header;
        private BamSubHeader _subHeader;

        public List<ArchiveFile> Load(Stream input)
        {
            using var binaryReader = new BinaryReaderX(input, leaveOpen: true);

            // Read the header
            _header = ReadHeader(binaryReader);

            // Read the sub-header
            input.Position = _header.dataStart;
            _subHeader = ReadSubHeader(binaryReader);

            // Calculate the aligned file offset
            long fileOffset = AlignTo(input.Position, 0x80);

            // Create a substream for the file data
            var fileStream = new SubStream(input, fileOffset, _subHeader.size);

            List<ArchiveFile> files =
            [
                new(new ArchiveFileInfo
                {
                    FilePath = $"00000000{BamSupport.DetermineExtension(fileStream)}",
                    FileData = fileStream
                })

            ];

            return files;
        }

        public void Save(Stream output, ArchiveFile file)
        {
            using var binaryWriter = new BinaryWriterX(output);

            // Calculate offsets
            long subHeaderOffset = _header.dataStart;
            long fileOffset = AlignTo(subHeaderOffset + SubHeaderSize, 0x80);

            // Write the file data
            output.Position = fileOffset;
            _ = file.WriteFileData(binaryWriter.BaseStream, false);
            binaryWriter.WriteAlignment(0x80);

            // Write the sub-header with updated size
            output.Position = subHeaderOffset;
            _subHeader.size = (int)file.FileSize;
            WriteSubHeader(_subHeader, binaryWriter);

            // Write the header with updated overall size
            output.Position = 0;
            _header.size = (int)output.Length;
            WriteHeader(_header, binaryWriter);
        }

        private BamHeader ReadHeader(BinaryReaderX reader)
        {
            return new BamHeader
            {
                magic = reader.ReadString(4),
                size = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                zero2 = reader.ReadInt32(),
                dataStart = reader.ReadInt32()
            };
        }

        private BamSubHeader ReadSubHeader(BinaryReaderX reader)
        {
            return new BamSubHeader
            {
                magic = reader.ReadString(8),
                size = reader.ReadInt32()
            };
        }

        private void WriteHeader(BamHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.size);
            writer.Write(header.zero0);
            writer.Write(header.zero1);
            writer.Write(header.zero2);
            writer.Write(header.dataStart);
        }

        private void WriteSubHeader(BamSubHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.size);
        }

        private static long AlignTo(long position, int alignment)
        {
            return (position + alignment - 1) / alignment * alignment;
        }
    }
}
