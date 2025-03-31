using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class Xbb
    {
        private const int HeaderSize_ = 0x20;
        private const int EntrySize_ = 0x10;
        private const int HashEntrySize_ = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = typeReader.Read<XbbHeader>(br);

            // Read entries
            var entries = typeReader.ReadMany<XbbFileEntry>(br, header.entryCount);

            // Read hash entries
            var hashEntries = typeReader.ReadMany<XbbHashEntry>(br, header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);

                br.BaseStream.Position = entry.nameOffset;
                var name = br.ReadNullTerminatedString();

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var entryPosition = HeaderSize_;
            var hashEntryPosition = entryPosition + files.Count * EntrySize_;
            var namePosition = hashEntryPosition + files.Count * HashEntrySize_;

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Write names
            bw.BaseStream.Position = namePosition;

            var nameDictionary = new Dictionary<UPath, int>();
            foreach (var file in files)
            {
                if (!nameDictionary.ContainsKey(file.FilePath))
                    nameDictionary.Add(file.FilePath, (int)bw.BaseStream.Position);

                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);
            }

            var dataPosition = (bw.BaseStream.Position + 0x7F) & ~0x7F;

            // Write files
            bw.BaseStream.Position = dataPosition;

            var xbbHash = new Kryptography.Checksum.Xbb();
            var fileEntries = new List<XbbFileEntry>();
            var hashEntries = new List<XbbHashEntry>();
            foreach (var file in files)
            {
                var offset = bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(0x80);

                var hash = xbbHash.ComputeValue(file.FilePath.ToRelative().FullName);
                fileEntries.Add(new XbbFileEntry
                {
                    offset = (int)offset,
                    size = (int)writtenSize,
                    nameOffset = nameDictionary[file.FilePath],
                    hash = hash
                });

                hashEntries.Add(new XbbHashEntry
                {
                    hash = hash,
                    index = fileEntries.Count - 1
                });
            }

            // Write file entries
            bw.BaseStream.Position = entryPosition;
            typeWriter.WriteMany(fileEntries, bw);

            // Write hash entries
            bw.BaseStream.Position = hashEntryPosition;
            typeWriter.WriteMany(hashEntries.OrderBy(x => x.hash), bw);

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new XbbHeader
            {
                entryCount = files.Count
            }, bw);
        }
    }
}
