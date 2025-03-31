using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Fnv;

namespace plugin_nintendo.Archives
{
    class Pac
    {
        private const int HeaderSize_ = 0x10;
        private const int TableInfoSize_ = 0x34;
        private const int AssetSize_ = 0x10;
        private const int EntrySize_ = 0x30;

        private PacHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = typeReader.Read<PacHeader>(br);

            // Read table info
            var tableInfo = typeReader.Read<PacTableInfo>(br);

            // Read assets
            input.Position = tableInfo.assetOffset;
            var assets = typeReader.ReadMany<PacAsset>(br, tableInfo.assetCount);

            // Read entries
            input.Position = tableInfo.entryOffset;
            var entries = typeReader.ReadMany<PacEntry>(br, tableInfo.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var asset in assets)
            {
                input.Position = asset.stringOffset;
                var assetName = br.ReadNullTerminatedString();

                var entryStartCount = (asset.entryOffset - tableInfo.entryOffset) / EntrySize_;
                foreach (var entry in entries.Skip(entryStartCount).Take(asset.count))
                {
                    input.Position = entry.stringOffset;
                    var entryName = br.ReadNullTerminatedString();

                    var subStream = new SubStream(input, entry.offset, entry.compSize);
                    var fileName = assetName + "/" + entryName;

                    result.Add(new PacArchiveFile(new CompressedArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = subStream,
                        Compression = Kompression.Compressions.ZLib.Build(),
                        DecompressedSize = entry.decompSize
                    }, entry));
                }
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);
            
            var hash = Fnv1.Create();

            // Get distinct strings
            var stringMap = GetStringMap(files);

            // Calculate offsets
            var tableInfoOffset = HeaderSize_;
            var assetOffset = (tableInfoOffset + TableInfoSize_ + 0x3F) & ~0x3F;
            var entryOffset = (assetOffset + files.Select(x => x.FilePath.GetFirstDirectory(out _)).Distinct().Count() * AssetSize_ + 0x3F) & ~0x3F;
            var stringOffset = (entryOffset + files.Count * EntrySize_ + 0x3F) & ~0x3F;
            var fileOffset = (stringOffset + stringMap.Sum(x => x.Key.Length + 1) + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<PacEntry>();
            var fileMap = new Dictionary<uint, (long, long)>();
            var distinctFileCount = 0;

            var filePosition = fileOffset;
            foreach (var file in files.Cast<PacArchiveFile>().OrderBy(x => x.FilePath))
            {
                // Update entry data
                file.FilePath.ToRelative().GetFirstDirectory(out var filePath);

                file.Entry.decompSize = (int)file.FileSize;
                file.Entry.extensionOffset = (int)stringMap[file.FilePath.GetExtensionWithDot()] + stringOffset;
                file.Entry.extensionFnvHash = hash.ComputeValue(file.FilePath.GetExtensionWithDot());
                file.Entry.stringOffset = (int)stringMap[filePath.FullName] + stringOffset;
                file.Entry.fnvHash = hash.ComputeValue(filePath.FullName);

                // Check if file already exists
                var fileHash = file.GetHash();
                if (fileMap.ContainsKey(fileHash))
                {
                    file.Entry.offset = (int)fileMap[fileHash].Item1;
                    file.Entry.compSize = file.Entry.compSize2 = (int)fileMap[fileHash].Item2;

                    entries.Add(file.Entry);
                    continue;
                }

                // Write file data
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.offset = filePosition;
                file.Entry.compSize = file.Entry.compSize2 = (int)writtenSize;

                entries.Add(file.Entry);
                fileMap[fileHash] = (filePosition, writtenSize);
                distinctFileCount++;

                filePosition += (int)writtenSize;
            }
            bw.WriteAlignment(16);

            // Write strings
            output.Position = stringOffset;
            foreach (var pair in stringMap)
                bw.WriteString(pair.Key, Encoding.ASCII);

            // Write entries
            output.Position = entryOffset;
            typeWriter.WriteMany(entries, bw);

            // Write assets
            var entryPosition = entryOffset;
            var assetCount = 0;

            output.Position = assetOffset;
            foreach (var fileGroup in files.OrderBy(x => x.FilePath).GroupBy(x => x.FilePath.GetFirstDirectory(out _)))
            {
                var fileCount = fileGroup.Count();
                typeWriter.Write(new PacAsset
                {
                    count = fileCount,
                    entryOffset = entryPosition,
                    stringOffset = (int)stringMap[fileGroup.Key] + stringOffset,
                    fnvHash = hash.ComputeValue(fileGroup.Key)
                }, bw);

                entryPosition += fileCount * EntrySize_;
                assetCount++;
            }

            // Write table info
            output.Position = tableInfoOffset;
            typeWriter.Write(new PacTableInfo
            {
                fileOffset = fileOffset,
                entryOffset = entryOffset,
                stringOffset = stringOffset,
                assetOffset = assetOffset,
                unpaddedFileSize = (int)output.Length,
                fileCount = distinctFileCount,
                entryCount = entries.Count,
                stringCount = stringMap.Count,
                assetCount = assetCount
            }, bw);

            // Write header
            output.Position = 0;

            _header.dataOffset = fileOffset;
            typeWriter.Write(_header, bw);

            // Pad file to 0x1000
            output.Position = output.Length;
            bw.WriteAlignment(0x1000);
        }

        private IDictionary<string, long> GetStringMap(List<IArchiveFile> files)
        {
            var strings = files.Select(x =>
             {
                 x.FilePath.ToRelative().GetFirstDirectory(out var remaining);
                 return remaining.FullName;
             }).Distinct();
            strings = strings.Concat(files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct());
            strings = strings.Concat(files.Select(x => x.FilePath.GetFirstDirectory(out _)).Distinct());

            var stringPosition = 0;
            var stringMap = new Dictionary<string, long>();
            foreach (var str in strings)
            {
                stringMap[str] = stringPosition;
                stringPosition += str.Length + 1;
            }

            return stringMap;
        }
    }
}
