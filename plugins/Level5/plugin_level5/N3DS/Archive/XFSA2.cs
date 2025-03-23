using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.Compression;

namespace plugin_level5.N3DS.Archive
{
    class XFSA2 : IXfsa
    {
        private const int HeaderSize_ = 36;

        private XfsaHeader _header;

        public List<ArchiveFile> Load(Stream input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"), true);

            // Header
            var header = typeReader.Read<XfsaHeader>(br);
            if (header == null)
                return new List<ArchiveFile>();

            _header = header;

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
            var result = new List<ArchiveFile>();
            foreach (var directory in directoryEntries)
            {
                names.BaseStream.Position = directory.directoryNameOffset;
                var directoryName = names.ReadNullTerminatedString();

                var filesInDirectory = entries.Skip(directory.FirstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.FileOffset, file.FileSize);

                    names.BaseStream.Position = directory.fileNameStartOffset + file.NameOffset;
                    var fileName = names.ReadNullTerminatedString();

                    var fileInfo = new ArchiveFileInfo
                    {
                        FileData = fileStream,
                        FilePath = directoryName + fileName,
                        PluginIds = XfsaSupport.RetrievePluginMapping(fileStream, fileName)
                    };

                    result.Add(new XfsaArchiveFile<Xfsa2FileEntry>(fileInfo, file));
                }
            }

            return result;
        }

        public void Save(Stream output, List<ArchiveFile> files)
        {
            // Build directory, file, and name tables
            BuildTables(files, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = HeaderSize_;

            // Write directory entries
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = HeaderSize_;

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

            XfsaSupport.WriteCompressedTableEntries(bw.BaseStream, fileEntries.Select(x => ((XfsaArchiveFile<Xfsa2FileEntry>)x).Entry));
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            XfsaSupport.Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;

            foreach (var file in fileEntries)
            {
                var fileEntry = (XfsaArchiveFile<Xfsa2FileEntry>)file;

                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.FileOffset;
                fileEntry.WriteFileData(bw.BaseStream, false);

                bw.WriteAlignment(16);
            }

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(_header, bw);
        }

        private void BuildTables(IEnumerable<ArchiveFile> files,
            out IList<Xfsa2DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<ArchiveFile> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Crc32B;
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<ArchiveFile>();
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

                var hash = crc32.ComputeValue(directoryName.ToLower(), sjis);
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
                foreach (var file in fileGroupEntries)
                {
                    var fileEntry = (XfsaArchiveFile<Xfsa2FileEntry>)file;
                    fileEntry.Entry.NameOffset = (int)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    fileEntry.Entry.crc32 = crc32.ComputeValue(fileEntry.FilePath.GetName().ToLower(), sjis);
                    fileEntry.Entry.FileOffset = fileOffset;
                    fileEntry.Entry.FileSize = (int)fileEntry.FileSize;

                    fileOffset = (int)((fileOffset + fileEntry.FileSize + 15) & ~15);

                    nameBw.WriteString(fileEntry.FilePath.GetName(), sjis, false);
                }

                // Add file entries in order of ascending hash
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => ((XfsaArchiveFile<Xfsa2FileEntry>)x).Entry.crc32));
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
