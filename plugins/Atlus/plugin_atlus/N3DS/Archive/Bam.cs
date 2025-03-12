using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;

namespace plugin_atlus.N3DS.Archive
{
    class Bam
    {
        private const int SubHeaderSize = 12;

        private BamHeader _header;
        private BamSubHeader _subHeader;

        public List<BamArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var binaryReader = new BinaryReaderX(input, leaveOpen: true);

            // Read the header
            _header = typeReader.Read<BamHeader>(binaryReader);

            // Read the sub-header
            input.Position = _header.dataStart;
            _subHeader = typeReader.Read<BamSubHeader>(binaryReader);

            // Calculate the aligned file offset
            long fileOffset = AlignTo(input.Position, 0x80);

            // Create a substream for the file data
            var fileStream = new SubStream(input, fileOffset, _subHeader.size);

            var files = new List<BamArchiveFile>();
            files.Add(new BamArchiveFile(new ArchiveFileInfo {
                FilePath = $"00000000{BamSupport.DetermineExtension(fileStream)}",
                FileData = fileStream
            }, new BamFileInfo()));

            return files;
        }

        public void Save(Stream output, BamArchiveFile file)
        {
            var typeWriter = new BinaryTypeWriter();
            using var binaryWriter = new BinaryWriterX(output);

            // Calculate offsets
            long subHeaderOffset = _header.dataStart;
            long fileOffset = AlignTo(subHeaderOffset + SubHeaderSize, 0x80);

            // Write the file data
            output.Position = fileOffset;            
            var writtenSize = file.WriteFileData(binaryWriter.BaseStream, false);
            binaryWriter.WriteAlignment(0x80);

            // Write the sub-header with updated size
            output.Position = subHeaderOffset;
            _subHeader.size = (int)file.FileSize;
            typeWriter.Write(_subHeader, binaryWriter);

            // Write the header with updated overall size
            output.Position = 0;
            _header.size = (int)output.Length;
            typeWriter.Write(_header, binaryWriter);
        }

        private static long AlignTo(long position, int alignment)
        {
            return (position + alignment - 1) / alignment * alignment;
        }
    }
}
