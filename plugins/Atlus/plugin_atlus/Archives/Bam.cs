using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_atlus.Archives
{
    class Bam
    {
        private static readonly int SubHeaderSize = Tools.MeasureType(typeof(BamSubHeader));

        private BamHeader _header;
        private BamSubHeader _subHeader;

        public ArchiveFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<BamHeader>();

            // Read sub header
            input.Position = _header.dataStart;
            _subHeader = br.ReadType<BamSubHeader>();

            // Add file
            var fileOffset = (input.Position + 0x7F) & ~0x7F;

            var subStream = new SubStream(input, fileOffset, _subHeader.size);
            var name = $"00000000{BamSupport.DetermineExtension(subStream)}";

            return new ArchiveFileInfo(subStream, name);
        }

        public void Save(Stream output, ArchiveFileInfo file)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var subHeaderOffset = _header.dataStart;
            var fileOffset = (subHeaderOffset + SubHeaderSize + 0x7F) & ~0x7F;

            // Write file
            output.Position = fileOffset;

            var writtenSize = file.SaveFileData(output);
            bw.WriteAlignment(0x80);

            // Write sub header
            output.Position = subHeaderOffset;

            _subHeader.size = (int)writtenSize;
            bw.WriteType(_subHeader);

            // Write header
            output.Position = 0;
            _header.size = (int)output.Length;
            bw.WriteType(_header);
        }
    }
}
