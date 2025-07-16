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
            using var reader = new BinaryReaderX(ddtStream, EucJpEncoding);

            return EnumerateFiles(reader, imgStream, UPath.Root).ToList();
        }

        public void Save(Stream ddtStream, Stream imgStream, IList<IArchiveFile> files)
        {
            using var writer = new BinaryWriterX(ddtStream);

            var fileTree = files.ToTree();

            // Write entries below root
            writer.BaseStream.Position = EntrySize_;
            WriteEntries(writer, fileTree, imgStream);

            // Write root
            var root = new DdtEntry
            {
                nameOffset = 0,
                entryOffset = EntrySize_,
                entrySize = -(fileTree.Directories.Count + fileTree.Files.Count)
            };

            writer.BaseStream.Position = 0;
            WriteEntry(root, writer);
        }

        private IEnumerable<IArchiveFile> EnumerateFiles(BinaryReaderX reader, Stream imgStream, UPath currentPath)
        {
            // Read current entry
            var entry = ReadEntry(reader);

            // Read name
            reader.BaseStream.Position = entry.nameOffset;
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
                    foreach (IArchiveFile file in EnumerateFiles(reader, imgStream, currentPath / name))
                        yield return file;
                }
            }
        }

        private long WriteEntries(BinaryWriterX writer, DirectoryEntry entry, Stream imgStream)
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
                entryEndOffset = (uint)WriteEntries(writer, directory.Directory, imgStream);
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
                WriteEntry(infoHolder.Entry, writer);

            return entryEndOffset;
        }

        private DdtEntry ReadEntry(BinaryReaderX reader)
        {
            return new DdtEntry
            {
                nameOffset = reader.ReadUInt32(),
                entryOffset = reader.ReadUInt32(),
                entrySize = reader.ReadInt32()
            };
        }

        private void WriteEntry(DdtEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.entryOffset);
            writer.Write(entry.entrySize);
        }
    }
}
