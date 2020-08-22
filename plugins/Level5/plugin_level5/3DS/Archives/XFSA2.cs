using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5._3DS.Archives
{
    class XFSA2 : IXfsa
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(XfsaHeader));

        private XfsaHeader _header;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = br.ReadType<XfsaHeader>();

            // Read directory entries
            var directoryEntries = XfsaSupport.ReadCompressedTableEntries<Xfsa2DirectoryEntry>(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset,
                _header.directoryEntriesCount);

            // Read directory hashes
            var directoryHashes = XfsaSupport.ReadCompressedTableEntries<uint>(input,
                _header.directoryHashOffset, _header.fileEntriesOffset - _header.directoryHashOffset,
                _header.directoryHashCount);

            // Read file entry table
            var entries = XfsaSupport.ReadCompressedTableEntries<Xfsa2FileEntry>(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset,
                _header.fileEntriesCount);

            // Read nameTable
            var nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);

            // Add Files
            var names = new BinaryReaderX(nameStream);
            var result = new List<ArchiveFileInfo>();
            foreach (var directory in directoryEntries)
            {
                names.BaseStream.Position = directory.directoryNameOffset;
                var directoryName = names.ReadCStringSJIS();

                var filesInDirectory = entries.Skip(directory.FirstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.FileOffset, file.FileSize);

                    names.BaseStream.Position = directory.fileNameStartOffset + file.NameOffset;
                    var fileName = names.ReadCStringSJIS();

                    result.Add(new XfsaArchiveFileInfo<Xfsa2FileEntry>(fileStream, directoryName + fileName, file)
                    {
                        PluginIds = XfsaSupport.RetrievePluginMapping(fileStream, fileName)
                    });
                }
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files, IProgressContext progress)
        {
            // Group files by directory
            var castedFiles = files.Cast<XfsaArchiveFileInfo<Xfsa2FileEntry>>();

            // Build directory, file, and name tables
            BuildTables(castedFiles, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = _headerSize;

            // Write directory entries
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = _headerSize;

            XfsaSupport.WriteCompressedTableEntries(bw.BaseStream, directoryEntries);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            XfsaSupport.WriteCompressedTableEntries(bw.BaseStream, directoryHashes);
            bw.WriteAlignment(4);

            // Write file entry hashes
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            XfsaSupport.WriteCompressedTableEntries(bw.BaseStream, fileEntries.Select(x => x.Entry));
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            XfsaSupport.Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;

            foreach (var fileEntry in fileEntries)
            {
                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.FileOffset;
                fileEntry.SaveFileData(bw.BaseStream, null);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void BuildTables(IEnumerable<XfsaArchiveFileInfo<Xfsa2FileEntry>> files,
            out IList<Xfsa2DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<XfsaArchiveFileInfo<Xfsa2FileEntry>> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Create(Crc32Formula.Normal);
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<XfsaArchiveFileInfo<Xfsa2FileEntry>>();
            directoryEntries = new List<Xfsa2DirectoryEntry>();
            directoryHashes = new List<uint>();
            var fileOffset = 0;
            foreach (var fileGroup in groupedFiles)
            {
                var fileIndex = fileInfos.Count;
                var fileGroupEntries = fileGroup.ToArray();

                // Add directory entry first
                var directoryNameOffset = (int)nameBw.BaseStream.Position;
                var directoryName = fileGroup.Key.ToRelative().FullName;
                if (!string.IsNullOrEmpty(directoryName))
                    directoryName += "/";
                nameBw.WriteString(directoryName, sjis, false);

                var hash = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(sjis.GetBytes(directoryName.ToLower())));
                var newDirectoryEntry = new Xfsa2DirectoryEntry
                {
                    crc32 = string.IsNullOrEmpty(fileGroup.Key.ToRelative().FullName) ? 0xFFFFFFFF : hash,

                    directoryCount = (short)groupedFiles.Count(gf => fileGroup.Key != gf.Key && gf.Key.IsInDirectory(fileGroup.Key, false)),

                    fileCount = (short)fileGroupEntries.Length,
                    FirstFileIndex = (short)fileIndex,

                    directoryNameOffset = directoryNameOffset,
                    fileNameStartOffset = (int)nameBw.BaseStream.Position
                };
                if (newDirectoryEntry.crc32 != 0xFFFFFFFF)
                    directoryHashes.Add(newDirectoryEntry.crc32);
                directoryEntries.Add(newDirectoryEntry);

                // Write file names in alphabetic order
                foreach (var fileEntry in fileGroupEntries)
                {
                    fileEntry.Entry.NameOffset = (int)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    fileEntry.Entry.crc32 = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(sjis.GetBytes(fileEntry.FilePath.GetName().ToLower())));
                    fileEntry.Entry.FileOffset = fileOffset;
                    fileEntry.Entry.FileSize = (int)fileEntry.FileSize;

                    fileOffset = (int)((fileOffset + fileEntry.FileSize + 15) & ~15);

                    nameBw.WriteString(fileEntry.FilePath.GetName(), sjis, false);
                }

                // Add file entries in order of ascending hash
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => x.Entry.crc32));
            }

            fileEntries = fileInfos;

            // Order directory entries by hash and set directoryIndex accordingly
            var directoryIndex = 0;
            directoryEntries = directoryEntries.OrderBy(x => x.crc32).Select(x =>
            {
                x.FirstDirectoryIndex = (short)directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }
    }
}
