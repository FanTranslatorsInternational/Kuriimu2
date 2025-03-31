using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class UMSBT
    {
        private const int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read first offset
            var firstOffset = br.ReadInt32();

            // Read entries
            input.Position = 0;

            var entries = new List<UMSBTEntry>();
            while (input.Position < firstOffset)
            {
                var entry = typeReader.Read<UMSBTEntry>(br);
                if (entry.size <= 0)
                    break;

                entries.Add(entry);
            }

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{i:00000000}.msbt";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = Math.Max(0x30, (files.Count * EntrySize + 0xF) & ~0xF);

            // Write files
            var entries = new List<UMSBTEntry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new UMSBTEntry
                {
                    offset = filePosition,
                    size = (int)writtenSize
                });

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = 0;
            typeWriter.WriteMany(entries, bw);
        }
    }
}
