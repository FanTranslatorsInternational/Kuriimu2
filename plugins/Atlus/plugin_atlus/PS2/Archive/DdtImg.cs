using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.PS2.Archive
{
    class DdtImg
    {
        private const int Alignment_ = 0x800;
        private const int EntrySize_ = 0xC;

        private static readonly Encoding EucJpEncoding = Encoding.GetEncoding("EUC-JP");

        public List<IArchiveFile> Load(Stream ddtStream, Stream imgStream)
        {
            var typeReader = new BinaryTypeReader();
            using var reader = new BinaryReaderX(ddtStream, EucJpEncoding);

            return EnumerateFiles(typeReader, reader, imgStream, UPath.Root).ToList();
        }

        public void Save(Stream ddtStream, Stream imgStream, IList<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var writer = new BinaryWriterX(ddtStream);

            var fileTree = files.ToTree();

            // Write entries below root
            writer.BaseStream.Position = EntrySize_;
            WriteEntries(typeWriter, writer, fileTree, imgStream);

            // Write root
            writer.BaseStream.Position = 0;
            typeWriter.Write(new DdtEntry
            {
                nameOffset = 0,
                entryOffset = (uint)EntrySize_,
                entrySize = -(fileTree.Directories.Count + fileTree.Files.Count)
            }, writer);
        }

        private IEnumerable<IArchiveFile> EnumerateFiles(BinaryTypeReader typeReader, BinaryReaderX reader, Stream imgStream, UPath currentPath)
        {
            // Read current entry
            var entry = typeReader.Read<DdtEntry>(reader);

            // Read name
            var name = reader.ReadNullTerminatedString();

            if (entry.entrySize >= 0)
            {
                // If entry is a file
                var subStream = new SubStream(imgStream, entry.entryOffset * Alignment_, entry.entrySize);

                yield return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = (currentPath / name).FullName,
                    FileData = subStream
                });
            }
            else
            {
                // If entry is a directory
                for (var i = 0; i < -entry.entrySize; i++)
                {
                    reader.BaseStream.Position = entry.entryOffset + i * EntrySize_;
                    foreach (IArchiveFile file in EnumerateFiles(typeReader, reader, imgStream, currentPath / name))
                        yield return file;
                }
            }
        }

        private long WriteEntries(BinaryTypeWriter typeWriter, BinaryWriterX writer, DirectoryEntry entry, Stream imgStream)
        {
            // Collect offsets
            var entryOffset = writer.BaseStream.Position;
            var stringOffset = entryOffset + (entry.Directories.Count + entry.Files.Count) * EntrySize_;
            var entryEndOffset = stringOffset +
                                 entry.Directories.Sum(x => EucJpEncoding.GetByteCount(x.Name) + 1) +
                                 entry.Files.Sum(x => EucJpEncoding.GetByteCount(x.FilePath.GetName()) + 1);
            entryEndOffset = (entryEndOffset + 0x3) & ~0x3;

            // Create holder entries
            var entries = entry.Directories.Select(x => new DdtInfoHolder(x))
                .Concat(entry.Files.Select(x => new DdtInfoHolder(x)))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToArray();

            // Write files
            foreach (var file in entries.Where(x => x.IsFile))
            {
                file.Entry.entryOffset = (uint)(imgStream.Position / Alignment_);
                file.Entry.entrySize = (int)file.File!.FileSize;

                file.File.WriteFileData(imgStream);

                while (imgStream.Position % Alignment_ != 0)
                    imgStream.WriteByte(0);
            }

            // Write deeper directory entries
            foreach (var directory in entries.Where(x => !x.IsFile))
            {
                directory.Entry.entryOffset = (uint)entryEndOffset;
                directory.Entry.entrySize = -(directory.Directory!.Directories.Count + directory.Directory.Files.Count);

                writer.BaseStream.Position = entryEndOffset;
                entryEndOffset = (uint)WriteEntries(typeWriter, writer, directory.Directory, imgStream);
            }

            // Write strings
            writer.BaseStream.Position = stringOffset;
            foreach (var infoHolder in entries)
            {
                infoHolder.Entry.nameOffset = (uint)writer.BaseStream.Position;
                writer.WriteString(infoHolder.Name, EucJpEncoding);
            }

            // Write current entries
            writer.BaseStream.Position = entryOffset;
            foreach (var infoHolder in entries)
                typeWriter.Write(infoHolder.Entry, writer);

            return entryEndOffset;
        }
    }
}
