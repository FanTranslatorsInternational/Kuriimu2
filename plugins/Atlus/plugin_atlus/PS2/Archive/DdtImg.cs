using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;


namespace plugin_atlus.PS2.Archive
{
    public class DdtImg
    {
        private const int Alignment_ = 0x800;

        private static readonly Encoding EucJpEncoding = Encoding.GetEncoding("EUC-JP");
        private static readonly int EntrySize = Marshal.SizeOf<DdtEntry>();

        public List<DdtArchiveFile> Load(Stream ddtStream, Stream imgStream)
        {
            using var br = new BinaryReaderX(ddtStream);
            var files = EnumerateFiles(br, imgStream, UPath.Root).ToList();
            return files;
        }

        public void Save(Stream ddtStream, Stream imgStream, List<DdtArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(ddtStream);

            /*var fileTree = files.ToTree();

            // Write entries below root
            bw.BaseStream.Position = EntrySize;
            WriteEntries(bw, fileTree, imgStream);

            // Write root
            bw.BaseStream.Position = 0;
            typeWriter.Write(new DdtEntry
            {
                nameOffset = 0,
                entryOffset = (uint)EntrySize,
                entrySize = -(fileTree.Directories.Count + fileTree.Files.Count)
            }, bw);*/
        }

        private IEnumerable<DdtArchiveFile> EnumerateFiles(BinaryReaderX br, Stream imgStream, UPath currentPath)
        {
            var typeReader = new BinaryTypeReader();

            // Read current entry
            var entry = typeReader.Read<DdtEntry>(br);

            // Read name
            br.BaseStream.Position = entry.nameOffset;
            var name = br.ReadNullTerminatedString();

            // If entry is a file
            if (entry.entrySize >= 0)
            {
                var subStream = new SubStream(imgStream, entry.entryOffset * Alignment_, entry.entrySize);

                yield return new DdtArchiveFile(new ArchiveFileInfo
                {
                    FilePath = (currentPath / name).FullName,
                    FileData = subStream
                }, entry);
                yield break;
            }

            // If entry is a directory
            for (var i = 0; i < -entry.entrySize; i++)
            {
                br.BaseStream.Position = entry.entryOffset + i * EntrySize;
                foreach (DdtArchiveFile file in EnumerateFiles(br, imgStream, currentPath / name))
                    yield return file;
            }
        }

        /*private long WriteEntries(BinaryWriterX bw, DirectoryEntry entry, Stream imgStream)
        {
            var typeWriter = new BinaryTypeWriter();

            // Collect offsets
            var entryOffset = bw.BaseStream.Position;
            var stringOffset = entryOffset + (entry.Directories.Count + entry.Files.Count) * EntrySize;
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
                file.Entry.entrySize = (int)file.File.FileSize;

                (file.File as ArchiveFileInfo).SaveFileData(imgStream);
                while (imgStream.Position % Alignment_ != 0)
                    imgStream.WriteByte(0);
            }

            // Write deeper directory entries
            foreach (var directory in entries.Where(x => !x.IsFile))
            {
                directory.Entry.entryOffset = (uint)entryEndOffset;
                directory.Entry.entrySize = -(directory.Directory.Directories.Count + directory.Directory.Files.Count);

                bw.BaseStream.Position = entryEndOffset;
                entryEndOffset = (uint)WriteEntries(bw, directory.Directory, imgStream);
            }

            // Write strings
            bw.BaseStream.Position = stringOffset;
            foreach (var infoHolder in entries)
            {
                infoHolder.Entry.nameOffset = (uint)bw.BaseStream.Position;
                bw.WriteString(infoHolder.Name, EucJpEncoding, false);
            }

            // Write current entries
            bw.BaseStream.Position = entryOffset;
            foreach (var infoHolder in entries)
                typeWriter.Write(infoHolder.Entry, bw);

            return entryEndOffset;
        }*/
    }
}
