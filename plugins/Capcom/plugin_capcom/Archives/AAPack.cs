using System.Text.RegularExpressions;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using plugin_capcom.Compression;

namespace plugin_capcom.Archives
{
    class AAPack
    {
        private static readonly int FileEntrySize = 0x14;
        private static readonly Regex FileNameRegex = new("\\d{8}\\.bin");

        public List<IArchiveFile> Load(Stream incStream, Stream datStream, string version)
        {
            using var incBr = new BinaryReaderX(incStream);

            var entryCount = (int)(incStream.Length / FileEntrySize);
            var entries = ReadEntries(incBr, entryCount);

            var nameMapping = AAPackSupport.GetMapping(version);

            var result = new List<IArchiveFile>(entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                var subStream = new SubStream(datStream, entries[i].offset, entries[i].compSize);

                var compressionMethod = NintendoCompressor.PeekCompressionMethod(subStream);

                var fileName = $"{i:00000000}.bin";
                if (nameMapping.ContainsKey(entries[i].hash))
                    fileName = nameMapping[entries[i].hash];

                result.Add(new AAPackArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream,
                    Compression = NintendoCompressor.GetConfiguration(compressionMethod),
                    DecompressedSize = (int)entries[i].uncompSize
                }, entries[i]));
            }

            return result;
        }

        public void Save(Stream incStream, Stream datStream, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(incStream);

            foreach (var file in files.Cast<AAPackArchiveFile>())
            {
                file.Entry.offset = (uint)datStream.Position;
                var writtenSize = file.WriteFileData(datStream, true);

                while (datStream.Position % 4 != 0)
                    datStream.WriteByte(0);

                file.Entry.hash = IsUnmappedFile(file.FilePath.ToRelative().FullName) ? file.Entry.hash : AAPackSupport.CreateHash(file.FilePath.ToRelative().FullName);
                file.Entry.compSize = (uint)writtenSize;
                file.Entry.uncompSize = (uint)file.FileSize;

                WriteEntry(file.Entry, bw);
            }
        }

        private bool IsUnmappedFile(string input)
        {
            return FileNameRegex.IsMatch(input);
        }

        private AAPackFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new AAPackFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private AAPackFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new AAPackFileEntry
            {
                offset = reader.ReadUInt32(),
                flags = reader.ReadUInt32(),
                uncompSize = reader.ReadUInt32(),
                compSize = reader.ReadUInt32(),
                hash = reader.ReadUInt32()
            };
        }

        private void WriteEntry(AAPackFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.flags);
            writer.Write(entry.uncompSize);
            writer.Write(entry.compSize);
            writer.Write(entry.hash);
        }
    }
}
