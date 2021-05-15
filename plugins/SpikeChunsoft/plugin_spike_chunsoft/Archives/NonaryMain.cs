using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kryptography;
using plugin_spike_chunsoft.Archives.Lookups;

namespace plugin_spike_chunsoft.Archives
{
    class NonaryMain
    {
        private static readonly byte[] Key = { 0xDA, 0xCE, 0xBA, 0xFA };

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(new PositionalXorStream(input, Key), true);

            // Read header
            var header = br.ReadType<NonaryHeader>();

            // Read directories
            var dirHeader = br.ReadType<NonaryTableHeader>();
            var dirHashes = br.ReadMultiple<uint>(dirHeader.entryCount);

            br.SeekAlignment();

            var dirEntries = br.ReadMultiple<NonaryDirectoryEntry>(dirHeader.entryCount);

            // Read file entries
            var fileHeader = br.ReadType<NonaryTableHeader>();
            var fileHashes = br.ReadMultiple<uint>(fileHeader.entryCount);
            br.SeekAlignment();

            // Add files
            var result = new List<IArchiveFileInfo>();

            foreach (var dirEntry in dirEntries)
            {
                if (!NonaryLookups.Directories.TryGetValue(dirEntry.directoryHash, out var dirPath))
                    dirPath = $"/UNK/0x{dirEntry.directoryHash:X8}";

                var fileEntries = br.ReadMultiple<NonaryEntry>(dirEntry.fileCount);
                foreach (var fileEntry in fileEntries)
                {
                    if (!NonaryLookups.Files.TryGetValue(fileEntry.XORpad, out var fileName))
                        fileName = $"{dirPath}/0x{fileEntry.XORpad:X8}.unk";

                    var subStream = new PositionalXorStream(new SubStream(input, header.dataOffset + fileEntry.fileOffset, fileEntry.fileSize), fileEntry.XorPadBytes);

                    result.Add(new ArchiveFileInfo(subStream, fileName));
                }
            }

            return result;
        }
    }
}
