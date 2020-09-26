using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_atlus.Archives
{
    class DdtImg
    {
        private const int Alignment_ = 0x800;

        private static readonly Encoding EucJpEncoding = Encoding.GetEncoding("EUC-JP");
        private static readonly int EntrySize = Tools.MeasureType(typeof(DdtEntry));

        public IList<IArchiveFileInfo> Load(Stream ddtStream, Stream imgStream)
        {
            using var br = new BinaryReaderX(ddtStream);
            return EnumerateFiles(br, imgStream, UPath.Root).ToArray();
        }

        public void Save(Stream ddtStream, Stream imgStream, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(ddtStream);

            var fileTree = files.ToTree();

            // Write entries below root
            bw.BaseStream.Position = EntrySize;
            WriteEntries(bw, fileTree, imgStream);

            // Write root
            bw.BaseStream.Position = 0;
            bw.WriteType(new DdtEntry
            {
                nameOffset = 0,
                entryOffset = (uint)EntrySize,
                entrySize = -(fileTree.Directories.Count + fileTree.Files.Count)
            });
        }

        private IEnumerable<IArchiveFileInfo> EnumerateFiles(BinaryReaderX br, Stream imgStream, UPath currentPath)
        {
            // Read current entry
            var entry = br.ReadType<DdtEntry>();

            // Read name
            var nameLength = GetStringLength(br, entry.nameOffset);
            br.BaseStream.Position = entry.nameOffset;
            var name = br.PeekString(nameLength, EucJpEncoding);

            // If entry is a file
            if (entry.IsFile)
            {
                var subStream = new SubStream(imgStream, entry.entryOffset * Alignment_, entry.entrySize);

                yield return new ArchiveFileInfo(subStream, (currentPath / name).FullName);
                yield break;
            }

            // If entry is a directory
            for (var i = 0; i < -entry.entrySize; i++)
            {
                br.BaseStream.Position = entry.entryOffset + i * EntrySize;
                foreach (var file in EnumerateFiles(br, imgStream, currentPath / name))
                    yield return file;
            }
        }

        private int GetStringLength(BinaryReaderX br, long offset)
        {
            var bkPos = br.BaseStream.Position;

            var result = 0;

            br.BaseStream.Position = offset;
            while (br.ReadByte() != 0)
                result++;

            br.BaseStream.Position = bkPos;
            return result;
        }

        private long WriteEntries(BinaryWriterX bw, DirectoryEntry entry, Stream imgStream)
        {
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
                bw.WriteType(infoHolder.Entry);

            return entryEndOffset;
        }
    }
}
