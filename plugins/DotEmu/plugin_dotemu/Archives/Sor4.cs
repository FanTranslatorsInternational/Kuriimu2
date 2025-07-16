using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_dotemu.Archives
{
    class Sor4
    {
        private Platform _platform;

        public List<IArchiveFile> Load(Stream texStream, Stream texListStream, Platform platform)
        {
            _platform = platform;

            using var texBr = new BinaryReaderX(texStream, true);
            using var texListBr = new BinaryReaderX(texListStream, Encoding.Unicode);

            // Read entries
            var entries = new List<Sor4Entry>();
            while (texListStream.Position < texListStream.Length)
                entries.Add(Sor4Support.ReadEntry(texListBr));

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                ArchiveFileInfo fileInfo;

                switch (platform)
                {
                    case Platform.Pc:
                        fileInfo = new ArchiveFileInfo
                        {
                            FilePath = entry.path,
                            FileData = new SubStream(texStream, entry.offset, entry.compSize)
                        };
                        break;

                    case Platform.Switch:
                        texStream.Position = entry.offset;
                        int decompSize = texBr.ReadInt32();

                        fileInfo = new CompressedArchiveFileInfo
                        {
                            FilePath = entry.path,
                            FileData = new SubStream(texStream, entry.offset + 4, entry.compSize - 4),
                            Compression = Compressions.Deflate.Build(),
                            DecompressedSize = decompSize
                        };
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported platform {platform}.");
                }

                result.Add(new Sor4ArchiveFile(fileInfo, entry));
            }

            return result;
        }

        public void Save(Stream texStream, Stream texListStream, IList<IArchiveFile> files)
        {
            using var texBw = new BinaryWriterX(texStream);
            using var texListBw = new BinaryWriterX(texListStream, Encoding.Unicode);

            // Write files
            var dataPosition = 0u;

            var entries = new List<Sor4Entry>();
            foreach (var file in files.Cast<Sor4ArchiveFile>())
            {
                texStream.Position = dataPosition;

                long writtenSize;
                switch (_platform)
                {
                    case Platform.Pc:
                        writtenSize = file.WriteFileData(texStream, true);

                        file.Entry.compSize = (int)writtenSize;
                        file.Entry.offset = dataPosition;

                        dataPosition = (uint)(dataPosition + writtenSize);
                        break;

                    case Platform.Switch:
                        texBw.Write((int)file.FileSize);
                        writtenSize = file.WriteFileData(texStream, true);

                        file.Entry.compSize = (int)writtenSize + 4;
                        file.Entry.offset = dataPosition;

                        dataPosition = (uint)((dataPosition + 4 + writtenSize + 0xF) & ~0xF);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported platform {_platform}.");
                }

                entries.Add(file.Entry);
            }

            // Write entries
            WriteEntries(entries, texListBw);
        }

        private void WriteEntries(IList<Sor4Entry> entries, BinaryWriterX writer)
        {
            foreach (Sor4Entry entry in entries)
                Sor4Support.WriteEntry(entry, writer);
        }
    }
}
